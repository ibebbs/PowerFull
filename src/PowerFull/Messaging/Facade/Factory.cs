using Microsoft.Extensions.Logging;
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
        private readonly ILogger<IFacade> _logger;

        public Factory(IOptions<Config> config, Mqtt.IFactory mqttFactory, ILogger<IFacade> logger)
        {
            _config = config;
            _mqttFactory = mqttFactory;
            _logger = logger;
        }

        public async ValueTask<IFacade> ForDevices(IEnumerable<IDevice> devices)
        {
            var implementation = new Implementation(_config.Value, _mqttFactory, devices, _logger);

            await implementation.InitializeAsync();

            return implementation;
        }
    }
}
