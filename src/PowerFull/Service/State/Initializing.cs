using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace PowerFull.Service.State
{
    public class Initializing : IState
    {
        private readonly Transition.IFactory _transitionFactory;
        private readonly IPayload _payload;

        public Initializing(Transition.IFactory transitionFactory, IPayload payload)
        {
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
                            deviceState => new
                            {
                                deviceState.Device,
                                deviceState.Priority,
                                PowerState = deviceState.PowerState == PowerState.Unknown
                                    ? _payload.MessagingFacade.GetPowerState(deviceState.Device)
                                    : Task.FromResult(deviceState.PowerState)
                            })
                        .ToArray();

                    await Task.WhenAll(deviceStates.Select(tuple => tuple.PowerState));

                    var payload = deviceStates
                        .Select(tuple => new Device.State(tuple.Device, tuple.Priority, tuple.PowerState.Result))
                        .ToArray();

                    observer.OnNext(_transitionFactory.ToRunning(new Payload(_payload.MessagingFacade, payload)));
                    observer.OnCompleted();
                }
            );
        }
    }
}
