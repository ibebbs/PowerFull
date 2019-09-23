using System;
using System.Collections.Generic;
using System.Text;

namespace PowerFull.Service.State.Transition
{
    public interface IFactory
    {
        ITransition ToInitializing(IPayload payload);

        ITransition ToRunning(IPayload payload);

        ITransition ToFaulted(Exception exception);

        ITransition ToStopped();
    }

    public class Factory : IFactory
    {
        public ITransition ToInitializing(IPayload payload)
        {
            return new ToInitializing(payload);
        }

        public ITransition ToRunning(IPayload payload)
        {
            return new ToRunning(payload);
        }

        public ITransition ToFaulted(Exception exception)
        {
            return new ToFaulted(exception);
        }

        public ITransition ToStopped()
        {
            return new ToStopped();
        }
    }
}
