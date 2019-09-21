using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mqtt;
using System.Text;
using System.Threading.Tasks;

namespace PowerFull.Messaging.Facade
{
    public interface IFactory
    {
        Task<IFacade> ForDevices(IEnumerable<IDevice> devices);
    }

    public class Factory : IFactory
    {
        private readonly IOptions<Config> _config;

        public Factory(IOptions<Config> config)
        {
            _config = config;
        }

        private async Task<IMqttClient> CreateMqttClient(IEnumerable<string> topics)
        {
            var config = new MqttConfiguration
            {
                Port = _config.Value.Port,
                MaximumQualityOfService = MqttQualityOfService.ExactlyOnce,
                AllowWildcardsInTopicFilters = true
            };

            var credentials = new MqttClientCredentials(
                _config.Value.ClientId,
                _config.Value.Username,
                _config.Value.Password
            );

            var client = await MqttClient.CreateAsync(_config.Value.Broker, config);
            var session = await client.ConnectAsync(credentials, cleanSession: true);

            foreach (string topic in topics)
            {
                await client.SubscribeAsync(topic, MqttQualityOfService.AtLeastOnce);
            }

            return client;
        }

        public async Task<IFacade> ForDevices(IEnumerable<IDevice> devices)
        {
            var client = await CreateMqttClient(devices.Select(d => d.PowerStateResponseTopic));

            return new Implementation(_config.Value, client, devices);
        }
    }
}
