using FakeItEasy;
using Microsoft.Reactive.Testing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;

namespace PowerFull.Tests.Service.State
{
    [TestFixture]
    public class RunningShould
    {
        private static readonly IDevice DeviceA = A.Fake<IDevice>();
        private static readonly IDevice DeviceB = A.Fake<IDevice>();

        [Test]
        public void EmitATransitionToRunningWhenAPowerEventTakesPlace()
        {
            var scheduler = new TestScheduler();

            var events = scheduler.CreateColdObservable(Recorded.OnNext(TimeSpan.FromMinutes(10), (Event.TurnOff, DeviceB)));
            var transitionFactory = A.Fake<PowerFull.Service.State.Transition.IFactory>();
            var transitionToRunning = A.Fake<PowerFull.Service.State.Transition.ToRunning>();
            A.CallTo(() => transitionFactory.ToRunning(A<PowerFull.Service.State.IPayload>.Ignored)).Returns(transitionToRunning);
            var logic = A.Fake<PowerFull.Service.State.ILogic>();
            A.CallTo(() => logic.GenerateEvents(A<IObservable<double>>.Ignored, A<IEnumerable<(IDevice, PowerState)>>.Ignored))
             .Returns(events);
            var deviceStates = new (IDevice, PowerState)[] { (DeviceA, PowerState.On), (DeviceB, PowerState.On) };
            var payload = A.Fake<PowerFull.Service.State.IPayload>();
            A.CallTo(() => payload.Devices).Returns(deviceStates);

            var subject = new PowerFull.Service.State.Running(transitionFactory, logic, payload);

            var actual = scheduler.Start(() => subject.Enter(), TimeSpan.FromMinutes(10).AsTestDisposalTime());

            var transitionsToRunning = actual.Messages
                .OfType<Recorded<Notification<PowerFull.Service.State.ITransition>>>()
                .Count(recorded => recorded.Value.Kind == NotificationKind.OnNext && recorded.Value.Value == transitionToRunning);

            Assert.That(transitionsToRunning, Is.EqualTo(1));
        }
    }
}
