using Microsoft.Reactive.Testing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;

namespace PowerFull.Tests
{
    public class LogicShould
    {
        private static Recorded<Notification<T>> OnNext<T>(TimeSpan timeSpan, T value)
        {
            return new Recorded<Notification<T>>(timeSpan.Ticks, Notification.CreateOnNext(value));
        }

        private static Recorded<Notification<T>> ExpectedOnNext<T>(TimeSpan timeSpan, T value)
        {
            return new Recorded<Notification<T>>(timeSpan.AsTestTime().Ticks, Notification.CreateOnNext(value));
        }

        private static Recorded<Notification<T>> OnCompleted<T>(TimeSpan timeSpan)
        {
            return new Recorded<Notification<T>>(timeSpan.Ticks, Notification.CreateOnCompleted<T>());
        }


        private static readonly IDevice DeviceA = new PowerFull.Device.Implementation { Id = "A" };
        private static readonly IDevice DeviceB = new PowerFull.Device.Implementation { Id = "B" };
        private static readonly IDevice DeviceC = new PowerFull.Device.Implementation { Id = "C" };

        private static IEnumerable<TestCaseData> TestCases
        {
            get
            {
                yield return new TestCaseData(
                    new[] { OnNext(TimeSpan.FromMinutes(5), 500.0) },
                    new[] { DeviceA, DeviceB },
                    new[] { DeviceC },
                    TimeSpan.FromMinutes(10),
                    new[] { ExpectedOnNext(TimeSpan.FromMinutes(10), (Event.TurnOn, DeviceA)) }
                    ).SetName("Expected Device To Be Powered On When 10 Minute Window Average Is Greater Than Turn On Limit");

                yield return new TestCaseData(
                    new[] { OnNext(TimeSpan.FromMinutes(5), -500.0) },
                    new[] { DeviceA, DeviceB },
                    new[] { DeviceC },
                    TimeSpan.FromMinutes(10),
                    new[] { ExpectedOnNext(TimeSpan.FromMinutes(10), (Event.TurnOff, DeviceC)) }
                    ).SetName("Expected Device To Be Powered Off When 10 Minute Window Average Is Less Than Turn Off Limit");

                yield return new TestCaseData(
                    new[] { OnNext(TimeSpan.FromMinutes(5), 500.0) },
                    new IDevice[0],
                    new[] { DeviceA, DeviceB, DeviceC },
                    TimeSpan.FromMinutes(10),
                    new Recorded<Notification<(Event, IDevice)>>[0]
                    ).SetName("Nothing When 10 Minute Window Average Is Greater Than Turn On Limit But No Devices Currently Off");

                yield return new TestCaseData(
                    new[] { OnNext(TimeSpan.FromMinutes(5), -500.0) },
                    new[] { DeviceA, DeviceB, DeviceC },
                    new IDevice[0],
                    TimeSpan.FromMinutes(10),
                    new Recorded<Notification<(Event, IDevice)>>[0]
                    ).SetName("Nothing When 10 Minute Window Average Is Less Than Turn Off Limit But No Devices Currently On");

                yield return new TestCaseData(
                    new[] { 
                        OnNext(TimeSpan.FromMinutes(5), 500.0), 
                        OnNext(TimeSpan.FromMinutes(15), -500.0) 
                    },
                    new[] { DeviceA, DeviceB },
                    new[] { DeviceC },
                    TimeSpan.FromMinutes(20),
                    new[] { 
                        ExpectedOnNext(TimeSpan.FromMinutes(10), (Event.TurnOn, DeviceA)),
                        ExpectedOnNext(TimeSpan.FromMinutes(20), (Event.TurnOff, DeviceA))
                    }).SetName("Expected Device To Be Powered On Then Off When Subsequent 10 Minute Window Averages Change");

                yield return new TestCaseData(
                    new[] {
                        OnNext(TimeSpan.FromMinutes(5), -500.0),
                        OnNext(TimeSpan.FromMinutes(15), 500.0)
                    },
                    new[] { DeviceA, DeviceB },
                    new[] { DeviceC },
                    TimeSpan.FromMinutes(20),
                    new[] {
                        ExpectedOnNext(TimeSpan.FromMinutes(10), (Event.TurnOff, DeviceC)),
                        ExpectedOnNext(TimeSpan.FromMinutes(20), (Event.TurnOn, DeviceC))
                    }).SetName("Expected Device To Be Powered Off Then On When Subsequent 10 Minute Window Averages Change");

                yield return new TestCaseData(
                    new[] {
                        OnNext(TimeSpan.FromMinutes(5), 500.0),
                        OnNext(TimeSpan.FromMinutes(15), 400.0)
                    },
                    new[] { DeviceA, DeviceB },
                    new[] { DeviceC },
                    TimeSpan.FromMinutes(20),
                    new[] {
                        ExpectedOnNext(TimeSpan.FromMinutes(10), (Event.TurnOn, DeviceA)),
                        ExpectedOnNext(TimeSpan.FromMinutes(20), (Event.TurnOn, DeviceB))
                    }).SetName("Devices To Be Powered On In Correct Order When Subsequent 10 Minute Window Averages Are Greater Than The Turn On Limit");

                yield return new TestCaseData(
                    new[] {
                        OnNext(TimeSpan.FromMinutes(5), -500.0),
                        OnNext(TimeSpan.FromMinutes(15), -400.0)
                    },
                    new[] { DeviceA },
                    new[] { DeviceC, DeviceB },
                    TimeSpan.FromMinutes(20),
                    new[] {
                        ExpectedOnNext(TimeSpan.FromMinutes(10), (Event.TurnOff, DeviceC)),
                        ExpectedOnNext(TimeSpan.FromMinutes(20), (Event.TurnOff, DeviceB))
                    }
                    ).SetName("Devices To Be Powered Off In Correct Order When Subsequent 10 Minute Window Averages Are Less Than The Turn Off Limit");
            }
        }

        [TestCaseSource(nameof(TestCases))]
        public void Emit(
            IEnumerable<Recorded<Notification<double>>> powerReadings,
            IEnumerable<IDevice> devicesCurrentlyPoweredOff,
            IEnumerable<IDevice> devicesCurrentlyPoweredOn,
            TimeSpan runForDuration,
            IEnumerable<Recorded<Notification<(Event, IDevice)>>> expected)
        {
            TestScheduler scheduler = new TestScheduler();

            var averages = scheduler.CreateHotObservable(powerReadings.ToArray());
            
            var observer = scheduler.Start(
                () => Logic.GenerateEvents(averages, devicesCurrentlyPoweredOn, devicesCurrentlyPoweredOff, scheduler),
                runForDuration
            );

            observer.Messages.AssertEqual(expected);
        }
    }
}