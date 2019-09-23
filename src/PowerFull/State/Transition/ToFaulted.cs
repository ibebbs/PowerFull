using System;

namespace PowerFull.State.Transition
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