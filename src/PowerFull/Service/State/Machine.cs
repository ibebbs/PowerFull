using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace PowerFull.Service.State
{
    public interface IMachine
    {
        IDisposable Initialize(IEnumerable<string> devices);
    }

    public class Machine : IMachine
    {
        private readonly IFactory _factory;
        private readonly ILogger<Machine> _logger;
        private readonly Subject<IState> _state;

        public Machine(IFactory factory, ILogger<Machine> logger)
        {
            _factory = factory;
            _logger = logger;
            _state = new Subject<IState>();
        }

        public IDisposable Initialize(IEnumerable<string> devices)
        {
            IObservable<ITransition> transitions = _state
                .StartWith(_factory.Starting(devices))
                .Do(state => _logger.LogInformation($"Entering state: '{state.GetType().Name}'"))
                .Select(state => state.Enter())
                .Switch()
                .Publish()
                .RefCount();

            IObservable<IState> states = Observable
                .Merge(
                    transitions.OfType<Transition.ToInitializing>().Select(transition => _factory.Initializing(transition.Payload)),
                    transitions.OfType<Transition.ToRunning>().Select(transition => _factory.Running(transition.Payload)),
                    transitions.OfType<Transition.ToFaulted>().Select(transition => _factory.Faulted(transition.Payload, transition.Exception)),
                    transitions.OfType<Transition.ToStopped>().Select(transition => _factory.Stopped()))
                .Do(transition => _logger.LogInformation($"Transitioning '{transition.GetType().Name}'"))
                .Publish()
                .RefCount();

            IObservable<IState> termination = states
                .OfType<Stopped>();

            return states
                .ObserveOn(Scheduler.CurrentThread)
                .TakeUntil(termination)
                .Subscribe(_state);
        }
    }
}
