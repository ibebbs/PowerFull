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
        private readonly ILogic _logic;
        private readonly IPayload _payload;
        private readonly Config _config;
        private readonly IScheduler _scheduler;

        public Running(Transition.IFactory transitionFactory, ILogic logic, IPayload payload, Config config, IScheduler scheduler)
        {
            _transitionFactory = transitionFactory;
            _logic = logic;
            _payload = payload;
            _config = config;
            _scheduler = scheduler;
        }

        public IObservable<(IDevice, PowerState)> PerformEvent((Event Event, IDevice Device) tuple)
        {
            switch (tuple.Event)
            {
                case Event.TurnOn:
                    return Observable.StartAsync(() => _payload.MessagingFacade.PowerOnAsync(tuple.Device)).Select(_ => (tuple.Device, PowerState.On));
                case Event.TurnOff:
                    return Observable.StartAsync(() => _payload.MessagingFacade.PowerOffAsync(tuple.Device)).Select(_ => (tuple.Device, PowerState.Off));
                default:
                    return Observable.Empty<(IDevice, PowerState)>();
            }
        }

        private IPayload Payload((IDevice Device, PowerState PowerState) tuple)
        {
            var newDevices = _payload.Devices
                .Where(d => d.Device == tuple.Device)
                .Select(d => new Device.State(d.Device, d.Priority, tuple.PowerState))
                .ToArray();

            var devices = _payload.Devices
                .GroupJoin(newDevices, d => d.Device, d => d.Device, (d, n) => n.FirstOption().Coalesce(() => d))
                .ToArray();

            return new Payload(_payload.MessagingFacade, devices);
        }

        private ITransition Transition(IPayload payload)
        {
            return _transitionFactory.ToRunning(payload);
        }

        private IObservable<ITransition> TransitionsFromRealPowerReadings()
        {
            return _logic
                .GenerateEvents(_payload.MessagingFacade.RealPower, _payload.Devices)
                .Select(PerformEvent)
                .Switch()
                .Select(Payload)
                .Select(Transition)
                .Catch<ITransition, Exception>(exception => Observable.Return(_transitionFactory.ToFaulted(_payload, exception)));
        }

        private IObservable<ITransition> TransitionsFromInactivity()
        {
            return Observable
                .Timer(TimeSpan.FromMinutes(_config.RequestDevicePowerStateAfterMinutes), _scheduler)
                .Select(_ => new Payload(_payload.MessagingFacade, _payload.Devices.Select(d => new Device.State(d.Device, d.Priority, PowerState.Unknown))))
                .Select(payload => _transitionFactory.ToInitializing(payload));
        }

        public IObservable<ITransition> Enter()
        {
            return Observable.Merge(
                TransitionsFromRealPowerReadings(),
                TransitionsFromInactivity()
            );
        }
    }
}
