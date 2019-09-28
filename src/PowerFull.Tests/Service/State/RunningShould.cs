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

        private static PowerFull.Service.State.ITransition TransitionToInitializing = A.Fake<PowerFull.Service.State.Transition.ToInitializing>();
        private static PowerFull.Service.State.ITransition TransitionToRunning = A.Fake<PowerFull.Service.State.Transition.ToRunning>();

        private static ITestableObservable<(Event, IDevice)> NullFactory(TestScheduler testScheduler)
        {
            return testScheduler.CreateColdObservable(new Recorded<Notification<(Event, IDevice)>>[0]);
        }

        private static ITestableObserver<PowerFull.Service.State.ITransition> RunSubject(
            IEnumerable<(IDevice, PowerState)> deviceStates,
            TimeSpan runFor,
            PowerFull.Service.Config config = null,
            Func<TestScheduler, ITestableObservable<(Event, IDevice)>> eventFactory = null)
        {
            var scheduler = new TestScheduler();

            eventFactory = eventFactory ?? NullFactory;
            config = config ?? new PowerFull.Service.Config();

            var transitionFactory = A.Fake<PowerFull.Service.State.Transition.IFactory>();
            A.CallTo(() => transitionFactory.ToInitializing(A<PowerFull.Service.State.IPayload>.Ignored)).Returns(TransitionToInitializing);
            A.CallTo(() => transitionFactory.ToRunning(A<PowerFull.Service.State.IPayload>.Ignored)).Returns(TransitionToRunning);
            var logic = A.Fake<PowerFull.Service.State.ILogic>();
            A.CallTo(() => logic.GenerateEvents(A<IObservable<double>>.Ignored, A<IEnumerable<(IDevice, PowerState)>>.Ignored))
             .Returns(eventFactory(scheduler));
            var payload = A.Fake<PowerFull.Service.State.IPayload>();
            A.CallTo(() => payload.Devices).Returns(deviceStates);

            var subject = new PowerFull.Service.State.Running(transitionFactory, logic, payload, config, scheduler);
            return scheduler.Start(() => subject.Enter(), runFor.AsTestDisposalTime());
        }

        [Test]
        public void EmitATransitionToRunningWhenAPowerEventTakesPlace()
        {
            var deviceStates = new (IDevice, PowerState)[] { (DeviceA, PowerState.On), (DeviceB, PowerState.On) };

            var actual = RunSubject(
                deviceStates,
                TimeSpan.FromMinutes(10),
                eventFactory: scheduler => scheduler.CreateColdObservable(Recorded.OnNext(TimeSpan.FromMinutes(10), (Event.TurnOff, DeviceB)))
            );

            var transitionsToRunning = actual.Messages
                .Count(recorded => recorded.Value.Kind == NotificationKind.OnNext && recorded.Value.Value == TransitionToRunning);

            Assert.That(transitionsToRunning, Is.EqualTo(1));
        }

        [Test]
        public void EmitATransitionToInitializingWhenAPowerEventHasNotOccuredInTheConfiguredPeriod()
        {
            var deviceStates = new (IDevice, PowerState)[] { (DeviceA, PowerState.On), (DeviceB, PowerState.On) };

            var actual = RunSubject(
                deviceStates,
                TimeSpan.FromMinutes(60),
                config: new PowerFull.Service.Config { RequestDevicePowerStateAfterMinutes = 60 }
            );

            var transitionsToInitializing = actual.Messages
                .Count(recorded => recorded.Value.Kind == NotificationKind.OnNext && recorded.Value.Value == TransitionToInitializing);

            Assert.That(transitionsToInitializing, Is.EqualTo(1));
        }
    }
}
