using System;

namespace PowerFull.State
{
    public interface IFactory
    {
        IState Starting(IPayload payload);
        IState Initializing(IPayload payload);
        IState Running(IPayload payload);
        IState Faulted(Exception exception);
        IState Stopped();
    }

    public class Factory : IFactory
    {
        private readonly Messaging.IFacade _messagingFacade;
        private readonly Transition.IFactory _transitionFactory;

        public Factory(Messaging.IFacade messagingFacade, Transition.IFactory transitionFactory)
        {
            _messagingFacade = messagingFacade;
            _transitionFactory = transitionFactory;
        }

        public IState Starting(IPayload payload)
        {
            return new Starting(_transitionFactory, payload);
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
