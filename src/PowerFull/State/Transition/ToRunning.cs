namespace PowerFull.State.Transition
{
    public class ToRunning : ITransition
    {
        public ToRunning(IPayload payload)
        {
            Payload = payload;
        }

        public IPayload Payload { get; }
    }
}