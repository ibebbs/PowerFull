using Bebbs.Monads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PowerFull.Messaging.Facade
{
    public class Implementation : IFacade
    {
        private readonly Config _config;
        private readonly IMqttClient _mqttClient;
        private readonly IReadOnlyDictionary<string, IDevice> _devices;
        private readonly IObservable<double> _realPower;

        public Implementation(Config config, IMqttClient mqttClient, IEnumerable<IDevice> devices)
        {
            _config = config;
            _mqttClient = mqttClient;
            _devices = devices.ToDictionary(kvp => kvp.Id);

            _realPower = _mqttClient.MessageStream
                .Where(message => message.Topic.Equals(config.PowerReadingTopic, StringComparison.OrdinalIgnoreCase))
                .Select(message => Encoding.UTF8.GetString(message.Payload))
                .Select(payload => Regex.Match(payload, config.PowerReadingPayloadValueRegex))
                .Where(match => match.Success)
                .Select(match => double.TryParse(match.Value, out double value) ? (double?)value : null)
                .Where(value => value != null)
                .Select(nullable => nullable.Value);
        }

        public async ValueTask DisposeAsync()
        {
            await _mqttClient.UnsubscribeAsync(_config.PowerReadingTopic);
            await _mqttClient.UnsubscribeAsync(_devices.Values.Select(d => d.PowerStateResponseTopic).ToArray());
            await _mqttClient.DisconnectAsync();

            _mqttClient.Dispose();
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

        public async Task<State> GetPowerState(IDevice device)
        {
            var response = PowerStateRespose(device);

            await _mqttClient.PublishAsync(
                new MqttApplicationMessage(
                    device.PowerStateRequestTopic,
                    Encoding.UTF8.GetBytes(device.PowerStateRequestPayload)
                ),
                MqttQualityOfService.AtLeastOnce
            );

            var state = await response;

            return state;
        }

        public IObservable<double> RealPower => _realPower;
    }
}
