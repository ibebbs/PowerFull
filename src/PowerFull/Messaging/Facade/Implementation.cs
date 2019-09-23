using Bebbs.Monads;
using Microsoft.Extensions.Logging;
using PowerFull.Messaging.Mqtt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PowerFull.Messaging.Facade
{
    public class Implementation : IFacade
    {
        private readonly Config _config;
        private readonly Mqtt.IFactory _mqttFactory;
        private readonly IReadOnlyDictionary<string, IDevice> _devices;
        private readonly ILogger<IFacade> _logger;
        private readonly Subject<double> _realPower;

        private IMqttClient _mqttClient;
        private IDisposable _subscription;

        public Implementation(Config config, Mqtt.IFactory mqttFactory, IEnumerable<IDevice> devices, ILogger<IFacade> logger)
        {
            _config = config;
            _mqttFactory = mqttFactory;
            _devices = devices.ToDictionary(kvp => kvp.Id);
            _logger = logger;

            _realPower = new Subject<double>();
        }

        private async ValueTask<IMqttClient> CreateMqttClient()
        {
            var credentials = new MqttClientCredentials(
                _config.ClientId,
                _config.Username,
                _config.Password
            );

            var client = await _mqttFactory.Create();
            var session = await client.ConnectAsync(credentials, cleanSession: true);

            var topics = _devices.Values
                .SelectMany(
                    device => new[]
                    {
                        device.PowerOffRequestTopic,
                        device.PowerOnRequestTopic,
                        device.PowerStateRequestTopic,
                        device.PowerStateResponseTopic
                    })
                .Concat(new[] { _config.PowerReadingTopic })
                .Distinct()
                .ToArray();

            foreach (string topic in topics)
            {
                await client.SubscribeAsync(topic, MqttQualityOfService.AtLeastOnce);
            }

            return client;
        }

        private Task<PowerState> PowerStateRespose(IDevice device)
        {
            var messages = _mqttClient.MessageStream
                .Where(message => message.Topic.Equals(device.PowerStateResponseTopic))
                .Select(message => Encoding.UTF8.GetString(message.Payload))
                .Publish()
                .RefCount();

            var onState = messages
                .Where(payload => Regex.IsMatch(payload, device.PowerStateResponseOnPayloadRegex))
                .Select(_ => PowerState.On);

            var offState = messages
                .Where(payload => Regex.IsMatch(payload, device.PowerStateResponseOffPayloadRegex))
                .Select(_ => PowerState.Off);

            var unknownState = messages
                .Timeout(TimeSpan.FromSeconds(10))
                .IgnoreElements()
                .Materialize()
                .Where(notification => notification.Exception != null)
                .Select(notification => PowerState.Unknown);

            return Observable.Merge(onState, offState, unknownState).Take(1).ToTask();
        }

        public async ValueTask InitializeAsync()
        {
            _mqttClient = await CreateMqttClient();

            _subscription = _mqttClient.MessageStream
                .Where(message => message.Topic.Equals(_config.PowerReadingTopic, StringComparison.OrdinalIgnoreCase))
                .Do(message => _logger.LogInformation($"Received power reading on topic '{_config.PowerReadingTopic}'"))
                .Where(message => message.Payload?.Length > 0)
                .Select(message => Encoding.UTF8.GetString(message.Payload))
                .Select(payload => Regex.Match(payload, _config.PowerReadingPayloadValueRegex))
                .Do(match => _logger.LogInformation($"{(match.Success ? "Successfully extracted" : "Failed to extract")} power value from message"))
                .Where(match => match.Success)
                .SelectMany(match => match.Groups.Cast<Group>())
                .Where(group => group.Name.Equals(Constants.RealPowerRegexGroupName, StringComparison.OrdinalIgnoreCase))
                .Select(group => double.TryParse(group.Value, out double value) ? (double?)value : null)
                .Do(value => _logger.LogInformation($"{(value != null ? "Successfully parsed" : "Failed to parse")} number value from message"))
                .Where(value => value != null)
                .Select(nullable => nullable.Value)
                .Do(value => _logger.LogInformation($"Received power reading of '{value}'"))
                .Subscribe(_realPower);
        }

        public async ValueTask DisposeAsync()
        {
            if (_subscription != null)
            {
                _subscription.Dispose();
                _subscription = null;
            }

            if (_mqttClient != null)
            {
                await _mqttClient.UnsubscribeAsync(_config.PowerReadingTopic);
                await _mqttClient.UnsubscribeAsync(_devices.Values.Select(d => d.PowerStateResponseTopic).ToArray());
                await _mqttClient.DisconnectAsync();

                _mqttClient.Dispose();
                _mqttClient = null;
            }
        }

        public async Task<PowerState> GetPowerState(IDevice device)
        {
            _logger.LogInformation($"Requesting power state for '{device.Id}' by publishing '{device.PowerStateRequestPayload ?? string.Empty}' to the topic '{device.PowerStateRequestTopic}'");

            var response = PowerStateRespose(device);

            var payload = string.IsNullOrWhiteSpace(device.PowerStateRequestPayload)
                ? null
                : Encoding.UTF8.GetBytes(device.PowerStateRequestPayload);

            await _mqttClient
                .PublishAsync(
                    new MqttApplicationMessage(device.PowerStateRequestTopic, payload),
                    MqttQualityOfService.AtLeastOnce)
                .ConfigureAwait(false);

            _logger.LogInformation($"Waiting for response from '{device.Id}' on the topic '{device.PowerStateResponseTopic}'");

            var state = await response.ConfigureAwait(false);

            _logger.LogInformation($"Received a power state of '{state}' for '{device.Id}'");

            return state;
        }

        public async Task PowerOnAsync(IDevice device)
        {
            _logger.LogInformation($"Requesting power on for '{device.Id}' by publishing '{device.PowerOnRequestPayload ?? string.Empty}' to the topic '{device.PowerOnRequestTopic}'");

            var payload = string.IsNullOrWhiteSpace(device.PowerOnRequestPayload)
                ? null
                : Encoding.UTF8.GetBytes(device.PowerOnRequestPayload);

            await _mqttClient
                .PublishAsync(
                    new MqttApplicationMessage(device.PowerOnRequestTopic, payload),
                    MqttQualityOfService.AtLeastOnce)
                .ConfigureAwait(false);
        }

        public async Task PowerOffAsync(IDevice device)
        {
            _logger.LogInformation($"Requesting power off for '{device.Id}' by publishing '{device.PowerOffRequestPayload ?? string.Empty}' to the topic '{device.PowerOffRequestTopic}'");

            var payload = string.IsNullOrWhiteSpace(device.PowerOffRequestPayload)
                ? null
                : Encoding.UTF8.GetBytes(device.PowerOffRequestPayload);

            await _mqttClient
                .PublishAsync(
                    new MqttApplicationMessage(device.PowerOffRequestTopic, payload),
                    MqttQualityOfService.AtLeastOnce)
                .ConfigureAwait(false);
        }

        public IObservable<double> RealPower => _realPower;
    }
}
