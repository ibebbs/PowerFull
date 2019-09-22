using Microsoft.Extensions.Options;
using System.Net.Mqtt;
using System.Threading.Tasks;

namespace PowerFull.Messaging.Mqtt
{
    public interface IFactory
    {
        ValueTask<IMqttClient> Create();
    }

    public class Factory : IFactory
    {
        private readonly IOptions<Config> _config;

        public Factory(IOptions<Config> config)
        {
            _config = config;
        }

        public async ValueTask<IMqttClient> Create()
        {
            var config = new MqttConfiguration
            {
                Port = _config.Value.Port,
                MaximumQualityOfService = MqttQualityOfService.ExactlyOnce,
                AllowWildcardsInTopicFilters = true
            };

            var client = await MqttClient.CreateAsync(_config.Value.Broker, config);

            return client;
        }
    }
}
