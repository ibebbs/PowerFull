using System;

namespace PowerFull.Service.State.Transition
{
    public class ToFaulted : ITransition
    {
        public ToFaulted(IPayload payload, Exception exception)
        {
            Payload = payload;
            Exception = exception;
        }

        public IPayload Payload { get; }

        public Exception Exception { get; }
    }
}