using Bebbs.Monads;
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
        private readonly Subject<double> _realPower;

        private IMqttClient _mqttClient;
        private IDisposable _subscription;

        public Implementation(Config config, Mqtt.IFactory mqttFactory, IEnumerable<IDevice> devices)
        {
            _config = config;
            _mqttFactory = mqttFactory;
            _devices = devices.ToDictionary(kvp => kvp.Id);

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

        private Task<State> PowerStateRespose(IDevice device)
        {
            var messages = _mqttClient.MessageStream
                .Where(message => message.Topic.Equals(device.PowerStateResponseTopic))
                .Select(message => Encoding.UTF8.GetString(message.Payload));

            var onState = messages
                .Where(payload => Regex.IsMatch(payload, device.PowerStateResponseOnPayloadRegex))
                .Select(_ => State.On);

            var offState = messages
                .Where(payload => Regex.IsMatch(payload, device.PowerStateResponseOffPayloadRegex))
                .Select(_ => State.Off);

            var unknownState = messages
                .Timeout(TimeSpan.FromSeconds(10))
                .IgnoreElements()
                .Materialize()
                .Where(notification => notification.Exception != null)
                .Select(notification => State.Unknown);

            return Observable.Merge(onState, offState, unknownState).ToTask();
        }

        public async ValueTask InitializeAsync()
        {
            _mqttClient = await CreateMqttClient();

            _subscription = _mqttClient.MessageStream
                .Where(message => message.Topic.Equals(_config.PowerReadingTopic, StringComparison.OrdinalIgnoreCase))
                .Select(message => Encoding.UTF8.GetString(message.Payload))
                .Select(payload => Regex.Match(payload, _config.PowerReadingPayloadValueRegex))
                .Where(match => match.Success)
                .Select(match => double.TryParse(match.Value, out double value) ? (double?)value : null)
                .Where(value => value != null)
                .Select(nullable => nullable.Value)
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

        public async ValueTask<State> GetPowerState(IDevice device)
        {
            var response = PowerStateRespose(device);

            var payload = string.IsNullOrWhiteSpace(device.PowerStateRequestPayload)
                ? null
                : Encoding.UTF8.GetBytes(device.PowerStateRequestPayload);

            await _mqttClient.PublishAsync(
                new MqttApplicationMessage(device.PowerStateRequestTopic, payload),
                MqttQualityOfService.AtLeastOnce
            );

            var state = await response;

            return state;
        }

        public IObservable<double> RealPower => _realPower;
    }
}
