using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace PowerFull.State
{
    public class Initializing : IState
    {
        private readonly Messaging.IFacade _messagingFacade;
        private readonly Transition.IFactory _transitionFactory;
        private readonly IPayload _payload;

        public Initializing(Messaging.IFacade messagingFacade, Transition.IFactory transitionFactory, IPayload payload)
        {
            _messagingFacade = messagingFacade;
            _transitionFactory = transitionFactory;
            _payload = payload;
        }

        public IObservable<ITransition> Enter()
        {
            return Observable.Create<ITransition>(
                async observer =>
                {
                    var deviceStates = _payload.Devices
                        .Select(
                            tuple => new
                            {
                                Device = tuple.Item1,
                                PowerState = tuple.Item2 == PowerState.Unknown
                                    ? _messagingFacade.GetPowerState(tuple.Item1)
                                    : Task.FromResult(tuple.Item2)
                            })
                        .ToArray();

                    await Task.WhenAll(deviceStates.Select(tuple => tuple.PowerState));

                    var payload = deviceStates
                        .Select(tuple => (tuple.Device, tuple.PowerState.Result))
                        .ToArray();

                    observer.OnNext(_transitionFactory.ToRunning(new Payload(payload)));
                    observer.OnCompleted();
                }
            );
        }
    }
}
