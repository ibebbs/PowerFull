using Microsoft.Extensions.Options;
using PowerFull.Messaging.Mqtt;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PowerFull.Messaging.Facade
{
    public interface IFactory
    {
        ValueTask<IFacade> ForDevices(IEnumerable<IDevice> devices);
    }

    public class Factory : IFactory
    {
        private readonly IOptions<Config> _config;
        private readonly Mqtt.IFactory _mqttFactory;

        public Factory(IOptions<Config> config, Mqtt.IFactory mqttFactory)
        {
            _config = config;
            _mqttFactory = mqttFactory;
        }

        public async ValueTask<IFacade> ForDevices(IEnumerable<IDevice> devices)
        {
            var implementation = new Implementation(_config.Value, _mqttFactory, devices);

            await implementation.InitializeAsync();

            return implementation;
        }
    }
}
