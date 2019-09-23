using Bebbs.Monads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerFull.Service.State
{
    public class Running : IState
    {
        private readonly Transition.IFactory _transitionFactory;
        private readonly IPayload _payload;

        public Running(Transition.IFactory transitionFactory, IPayload payload)
        {
            _transitionFactory = transitionFactory;
            _payload = payload;
        }

        public IObservable<(IDevice, PowerState)> PerformEvent((Event Event, IDevice Device) tuple)
        {
            switch (tuple.Event)
            {
                case Event.TurnOn:
                    return Observable.StartAsync(() => _payload.MessagingFacade.PowerOnAsync(tuple.Device)).Select(_ => (tuple.Device, PowerState.On));
                case Event.TurnOff:
                    return Observable.StartAsync(() => _payload.MessagingFacade.PowerOffAsync(tuple.Device)).Select(_ => (tuple.Device, PowerState.On));
                default:
                    return Observable.Empty<(IDevice, PowerState)>();
            }
        }

        private IPayload Payload((IDevice Device, PowerState powerState) tuple)
        {
            var devices = _payload.Devices
                .Where(t => t.Item1 != tuple.Device)
                .Concat(new[] { tuple });

            return new Payload(_payload.MessagingFacade, devices);
        }

        private ITransition Transition(IPayload payload)
        {
            return _transitionFactory.ToRunning(payload);
        }

        public IObservable<ITransition> Enter()
        {
            return Logic
                .GenerateEvents(_payload.MessagingFacade.RealPower, _payload.Devices, Scheduler.Default)
                .Select(PerformEvent)
                .Switch()
                .Select(Payload)
                .Select(Transition)
                .Catch<ITransition, Exception>(exception => Observable.Return(_transitionFactory.ToFaulted(_payload, exception)));
        }
    }
}
