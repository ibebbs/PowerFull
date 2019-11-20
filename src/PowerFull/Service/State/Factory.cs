using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;

namespace PowerFull.Service.State
{
    public interface IFactory
    {
        IState Starting(IEnumerable<string> devices);
        IState Initializing(IPayload payload);
        IState Running(IPayload payload);
        IState Faulted(IPayload payload, Exception exception);
        IState Stopped();
    }

    public class Factory : IFactory
    {
        private readonly Device.IFactory _deviceFactory;
        private readonly Messaging.Facade.IFactory _messagingFacadeFactory;
        private readonly Transition.IFactory _transitionFactory;
        private readonly ILogic _logic;
        private readonly IOptions<Config> _config;

        public Factory(Device.IFactory deviceFactory, Transition.IFactory transitionFactory, Messaging.Facade.IFactory messagingFacadeFactory, ILogic logic, IOptions<Config> config)
        {
            _deviceFactory = deviceFactory;
            _messagingFacadeFactory = messagingFacadeFactory;
            _transitionFactory = transitionFactory;
            _logic = logic;
            _config = config;
        }

        public IState Starting(IEnumerable<string> devices)
        {
            return new Starting(_transitionFactory, _deviceFactory, _messagingFacadeFactory, devices);
        }

        public IState Initializing(IPayload payload)
        {
            return new Initializing(_transitionFactory, payload);
        }

        public IState Running(IPayload payload)
        {
            return new Running(_transitionFactory, _logic, payload, _config.Value, Scheduler.Default);
        }

        public IState Faulted(IPayload payload, Exception exception)
        {
            return new Faulted(_transitionFactory, payload, exception);
        }

        public IState Stopped()
        {
            return new Stopped();
        }
    }
}
