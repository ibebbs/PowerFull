using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PowerFull.Service
{
    public class Implementation : IHostedService
    {
        private readonly State.IMachine _stateMachine;
        private readonly IOptions<Config> _config;

        private IDisposable _subscription;

        public Implementation(State.IMachine stateMachine, IOptions<Config> config)
        {
            _stateMachine = stateMachine;
            _config = config;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _subscription = _stateMachine.Initialize(_config.Value.Devices.Split(',', StringSplitOptions.RemoveEmptyEntries));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_subscription != null)
            {
                _subscription.Dispose();
                _subscription = null;
            }

            return Task.CompletedTask;
        }
    }
}
