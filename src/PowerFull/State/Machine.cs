using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace PowerFull.State
{
    public class Machine
    {
        private readonly IFactory _factory;
        private readonly Subject<IState> _state;

        public Machine(IFactory factory)
        {
            _factory = factory;
            _state = new Subject<IState>();
        }

        public IDisposable Initialize(IEnumerable<IDevice> devices)
        {
            var payload = new Payload(devices.Select(device => (device, PowerState.Unknown)));

            IObservable<ITransition> transitions = _state
                .StartWith(_factory.Starting(payload))
                .Select(state => state.Enter())
                .Switch()
                .Publish()
                .RefCount();

            IObservable<IState> states = Observable
                .Merge(
                    transitions.OfType<Transition.ToInitializing>().Select(transition => _factory.Initializing(transition.Payload)),
                    transitions.OfType<Transition.ToRunning>().Select(transition => _factory.Running(transition.Payload)),
                    transitions.OfType<Transition.ToFaulted>().Select(transition => _factory.Faulted(transition.Exception)),
                    transitions.OfType<Transition.ToStopped>().Select(transition => _factory.Stopped()))
                .Publish()
                .RefCount();

            IObservable<IState> termination = states.OfType<Stopped>();

            return states
                .ObserveOn(Scheduler.CurrentThread)
                .TakeUntil(termination)
                .Subscribe(_state);
        }
    }
}
