using System;

namespace PowerFull.Service.State.Transition
{
    public class ToFaulted : ITransition
    {
        public ToFaulted(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}