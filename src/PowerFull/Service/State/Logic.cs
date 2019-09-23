using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace PowerFull.Service.State
{
    public class Logic
    {
        private const double TurnOffLimit = -100;
        private const double TurnOnLimit = 300;

        public static IObservable<(Event, IDevice)> GenerateEvents(IObservable<double> realPower, IEnumerable<(IDevice, PowerState)> devices, IScheduler scheduler)
        {
            return Observable.Create<(Event, IDevice)>(
                observer =>
                {
                    var pendingOff = new Stack<IDevice>(devices.Where(tuple => tuple.Item2 == PowerState.On).Select(tuple => tuple.Item1).Reverse());
                    var pendingOn = new Stack<IDevice>(devices.Where(tuple => tuple.Item2 == PowerState.Off).Select(tuple => tuple.Item1).Reverse());

                    var windowAverage = realPower
                        .Buffer(TimeSpan.FromMinutes(10), scheduler)
                        .Select(buffer => buffer.Cast<double?>().Average() ?? 0.0)
                        .Publish()
                        .RefCount();

                    var powerOff = windowAverage
                        .Where(average => average < TurnOffLimit && pendingOff.Any())
                        .Select(average => pendingOff.Pop())
                        .Do(pendingOn.Push)
                        .Select(device => (Event.TurnOff, device));

                    var powerOn = windowAverage
                        .Where(average => average > TurnOnLimit && pendingOn.Any())
                        .Select(average => pendingOn.Pop())
                        .Do(pendingOff.Push)
                        .Select(device => (Event.TurnOn, device));

                    return Observable.Merge(powerOff, powerOn).Subscribe(observer);
                }
            );
        }
    }
}
