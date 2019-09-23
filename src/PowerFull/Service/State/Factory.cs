using System;
using System.Collections.Generic;

namespace PowerFull.Service.State
{
    public interface IFactory
    {
        IState Starting(IEnumerable<string> devices);
        IState Initializing(IPayload payload);
        IState Running(IPayload payload);
        IState Faulted(Exception exception);
        IState Stopped();
    }

    public class Factory : IFactory
    {
        private readonly Messaging.IFacade _messagingFacade;
        private readonly Device.IFactory _deviceFactory;
        private readonly Transition.IFactory _transitionFactory;

        public Factory(Messaging.IFacade messagingFacade, Device.IFactory deviceFactory, Transition.IFactory transitionFactory)
        {
            _messagingFacade = messagingFacade;
            _deviceFactory = deviceFactory;
            _transitionFactory = transitionFactory;
        }

        public IState Starting(IEnumerable<string> devices)
        {
            return new Starting(_transitionFactory, _deviceFactory, devices);
        }

        public IState Initializing(IPayload payload)
        {
            return new Initializing(_messagingFacade, _transitionFactory, payload);
        }

        public IState Running(IPayload payload)
        {
            return new Running(_messagingFacade, _transitionFactory, payload);
        }

        public IState Faulted(Exception exception)
        {
            return new Faulted(_transitionFactory, exception);
        }

        public IState Stopped()
        {
            return new Stopped();
        }
    }
}
