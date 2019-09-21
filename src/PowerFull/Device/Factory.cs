using Bebbs.Monads;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerFull.Device
{
    public class Factory
    {
        private static readonly IEnumerable<(Func<Config, string>, Func<ITheme, string>, Action<Implementation, string>)> Functors = new (Func<Config, string>, Func<ITheme, string>, Action<Implementation, string>)[]
        {
            (config => config.PowerStateRequestTopic, theme => theme.PowerStateRequestTopic, (device, value) => device.PowerStateRequestTopic = value),
            (config => config.PowerStateRequestPayload, theme => theme.PowerStateRequestPayload, (device, value) => device.PowerStateRequestPayload = value),
            (config => config.PowerStateResponseTopic, theme => theme.PowerStateResponseTopic, (device, value) => device.PowerStateResponseTopic = value),
            (config => config.PowerStateResponseOnPayloadRegex, theme => theme.PowerStateResponseOnPayloadRegex, (device, value) => device.PowerStateResponseOnPayloadRegex = value),
            (config => config.PowerStateResponseOffPayloadRegex, theme => theme.PowerStateResponseOffPayloadRegex, (device, value) => device.PowerStateResponseOffPayloadRegex = value),
            (config => config.PowerOnRequestTopic, theme => theme.PowerOnRequestTopic, (device, value) => device.PowerOnRequestTopic = value),
            (config => config.PowerOnRequestPayload, theme => theme.PowerOnRequestPayload, (device, value) => device.PowerOnRequestPayload = value),
            (config => config.PowerOffRequestTopic, theme => theme.PowerOffRequestTopic, (device, value) => device.PowerOffRequestTopic = value),
            (config => config.PowerOffRequestPayload, theme => theme.PowerOffRequestPayload, (device, value) => device.PowerOffRequestPayload = value)
        };

        private static Implementation Apply(Implementation implementation, string pattern, Action<Implementation, string> modifier)
        {
            var value = Pattern.Substitution(implementation, pattern);

            modifier(implementation, value);

            return implementation;
        }

        private readonly IOptions<Config> _config;

        public Factory(IOptions<Config> config)
        {
            _config = config;
        }

        private Func<Implementation, Implementation> Modifier(Func<Config, string> configLookup, Func<ITheme, string> themeLookup, Action<Implementation, string> modifier)
        {
            return configLookup(_config.Value)
                .AsOption()
                .Coalesce(() => Theme.Registry.For(_config.Value.Theme).Select(themeLookup))
                .Select(pattern => new Func<Implementation, Implementation>(implementation => Apply(implementation, pattern, modifier)))
                .Coalesce(() => new Func<Implementation, Implementation>(implementation => implementation));
        }

        public Task<IDevice> CreateDevice(string name)
        {
            var device = Functors
                .Select(tuple => Modifier(tuple.Item1, tuple.Item2, tuple.Item3))
                .Aggregate(new Implementation { Id = name }, (device, modifier) => modifier(device));

            return Task.FromResult<IDevice>(device);
        }
    }
}
