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
        private static readonly IDevice DeviceA = new PowerFull.Device.Implementation { Id = "A" };
        private static readonly IDevice DeviceB = new PowerFull.Device.Implementation { Id = "B" };
        private static readonly IDevice DeviceC = new PowerFull.Device.Implementation { Id = "C" };

        private static IEnumerable<TestCaseData> TestCases
        {
            get
            {
                yield return new TestCaseData(
                    new[] { Recorded.OnNext(TimeSpan.FromMinutes(5), 500.0) },
                    new[] { (DeviceA, PowerState.Off), (DeviceB, PowerState.Off), (DeviceC, PowerState.On) },
                    TimeSpan.FromMinutes(10),
                    new[] { Recorded.ExpectedOnNext(TimeSpan.FromMinutes(10), (Event.TurnOn, DeviceA)) }
                    ).SetName("Expected Device To Be Powered On When 10 Minute Window Average Is Greater Than Turn On Limit");

                yield return new TestCaseData(
                    new[] { Recorded.OnNext(TimeSpan.FromMinutes(5), -500.0) },
                    new[] { (DeviceA, PowerState.Off), (DeviceB, PowerState.Off), (DeviceC, PowerState.On) },
                    TimeSpan.FromMinutes(10),
                    new[] { Recorded.ExpectedOnNext(TimeSpan.FromMinutes(10), (Event.TurnOff, DeviceC)) }
                    ).SetName("Expected Device To Be Powered Off When 10 Minute Window Average Is Less Than Turn Off Limit");

                yield return new TestCaseData(
                    new[] { Recorded.OnNext(TimeSpan.FromMinutes(5), 500.0) },
                    new[] { (DeviceA, PowerState.On), (DeviceB, PowerState.On), (DeviceC, PowerState.On) },
                    TimeSpan.FromMinutes(10),
                    new Recorded<Notification<(Event, IDevice)>>[0]
                    ).SetName("Nothing When 10 Minute Window Average Is Greater Than Turn On Limit But No Devices Currently Off");

                yield return new TestCaseData(
                    new[] { Recorded.OnNext(TimeSpan.FromMinutes(5), -500.0) },
                    new[] { (DeviceA, PowerState.Off), (DeviceB, PowerState.Off), (DeviceC, PowerState.Off) },
                    TimeSpan.FromMinutes(10),
                    new Recorded<Notification<(Event, IDevice)>>[0]
                    ).SetName("Nothing When 10 Minute Window Average Is Less Than Turn Off Limit But No Devices Currently On");

                yield return new TestCaseData(
                    new[] {
                        Recorded.OnNext(TimeSpan.FromMinutes(5), 500.0),
                        Recorded.OnNext(TimeSpan.FromMinutes(15), -500.0) 
                    },
                    new[] { (DeviceA, PowerState.Off), (DeviceB, PowerState.Off), (DeviceC, PowerState.On) },
                    TimeSpan.FromMinutes(20),
                    new[] {
                        Recorded.ExpectedOnNext(TimeSpan.FromMinutes(10), (Event.TurnOn, DeviceA)),
                        Recorded.ExpectedOnNext(TimeSpan.FromMinutes(20), (Event.TurnOff, DeviceA))
                    }).SetName("Expected Device To Be Powered On Then Off When Subsequent 10 Minute Window Averages Change");

                yield return new TestCaseData(
                    new[] {
                        Recorded.OnNext(TimeSpan.FromMinutes(5), -500.0),
                        Recorded.OnNext(TimeSpan.FromMinutes(15), 500.0)
                    },
                    new[] { (DeviceA, PowerState.Off), (DeviceB, PowerState.Off), (DeviceC, PowerState.On) },
                    TimeSpan.FromMinutes(20),
                    new[] {
                        Recorded.ExpectedOnNext(TimeSpan.FromMinutes(10), (Event.TurnOff, DeviceC)),
                        Recorded.ExpectedOnNext(TimeSpan.FromMinutes(20), (Event.TurnOn, DeviceC))
                    }).SetName("Expected Device To Be Powered Off Then On When Subsequent 10 Minute Window Averages Change");

                yield return new TestCaseData(
                    new[] {
                        Recorded.OnNext(TimeSpan.FromMinutes(5), 500.0),
                        Recorded.OnNext(TimeSpan.FromMinutes(15), 400.0)
                    },
                    new[] { (DeviceA, PowerState.Off), (DeviceB, PowerState.Off), (DeviceC, PowerState.On) },
                    TimeSpan.FromMinutes(20),
                    new[] {
                        Recorded.ExpectedOnNext(TimeSpan.FromMinutes(10), (Event.TurnOn, DeviceA)),
                        Recorded.ExpectedOnNext(TimeSpan.FromMinutes(20), (Event.TurnOn, DeviceB))
                    }).SetName("Devices To Be Powered On In Correct Order When Subsequent 10 Minute Window Averages Are Greater Than The Turn On Limit");

                yield return new TestCaseData(
                    new[] {
                        Recorded.OnNext(TimeSpan.FromMinutes(5), -500.0),
                        Recorded.OnNext(TimeSpan.FromMinutes(15), -400.0)
                    },
                    new[] { (DeviceA, PowerState.Off), (DeviceC, PowerState.On), (DeviceB, PowerState.On) },
                    TimeSpan.FromMinutes(20),
                    new[] {
                        Recorded.ExpectedOnNext(TimeSpan.FromMinutes(10), (Event.TurnOff, DeviceC)),
                        Recorded.ExpectedOnNext(TimeSpan.FromMinutes(20), (Event.TurnOff, DeviceB))
                    }).SetName("Devices To Be Powered Off In Correct Order When Subsequent 10 Minute Window Averages Are Less Than The Turn Off Limit");
            }
        }

        [TestCaseSource(nameof(TestCases))]
        public void Emit(
            IEnumerable<Recorded<Notification<double>>> powerReadings,
            IEnumerable<(IDevice, PowerState)> devices,
            TimeSpan runForDuration,
            IEnumerable<Recorded<Notification<(Event, IDevice)>>> expected)
        {
            TestScheduler scheduler = new TestScheduler();

            var averages = scheduler.CreateHotObservable(powerReadings.ToArray());
            
            var observer = scheduler.Start(
                () => State.Logic.GenerateEvents(averages, devices, scheduler),
                runForDuration
            );

            observer.Messages.AssertEqual(expected);
        }
    }
}