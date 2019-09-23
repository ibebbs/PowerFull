namespace PowerFull.State.Transition
{
    public class ToInitializing : ITransition
    {
        public ToInitializing(IPayload payload)
        {
            Payload = payload;
        }

        public IPayload Payload { get; }
    }
}