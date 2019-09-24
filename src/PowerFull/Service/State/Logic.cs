using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace PowerFull.Service.State
{
    public interface ILogic
    {
        IObservable<(Event, IDevice)> GenerateEvents(IObservable<double> realPower, IEnumerable<(IDevice, PowerState)> devices);
    }

    public class Logic : ILogic
    {
        private readonly IOptions<Config> _config;
        private readonly IScheduler _scheduler;

        public Logic(IOptions<Config> config, IScheduler scheduler)
        {
            _config = config;
            _scheduler = scheduler;
        }

        public Logic(IOptions<Config> config) : this(config, Scheduler.Default) { }

        public IObservable<(Event, IDevice)> GenerateEvents(IObservable<double> realPower, IEnumerable<(IDevice, PowerState)> devices)
        {
            return Observable.Create<(Event, IDevice)>(
                observer =>
                {
                    var pendingOff = new Stack<IDevice>(devices.Where(tuple => tuple.Item2 == PowerState.On).Select(tuple => tuple.Item1).Reverse());
                    var pendingOn = new Stack<IDevice>(devices.Where(tuple => tuple.Item2 == PowerState.Off).Select(tuple => tuple.Item1).Reverse());

                    var windowAverage = realPower
                        .Buffer(TimeSpan.FromMinutes(_config.Value.PowerChangeAfterMinutes), _scheduler)
                        .Select(buffer => buffer.Cast<double?>().Average() ?? 0.0)
                        .Publish()
                        .RefCount();

                    var powerOff = windowAverage
                        .Where(average => average <= _config.Value.ThresholdToTurnOffDeviceWatts && pendingOff.Any())
                        .Select(average => pendingOff.Pop())
                        .Do(pendingOn.Push)
                        .Select(device => (Event.TurnOff, device));

                    var powerOn = windowAverage
                        .Where(average => average >= _config.Value.ThresholdToTurnOnDeviceWatts && pendingOn.Any())
                        .Select(average => pendingOn.Pop())
                        .Do(pendingOff.Push)
                        .Select(device => (Event.TurnOn, device));

                    return Observable
                        .Merge(powerOff, powerOn)
                        .Subscribe(observer);
                }
            );
        }
    }
}
