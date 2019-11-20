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

        private static ITestableObservable<(T1, T2)> NullFactory<T1, T2>(TestScheduler testScheduler)
        {
            return testScheduler.CreateColdObservable(new Recorded<Notification<(T1, T2)>>[0]);
        }

        private static ITestableObserver<PowerFull.Service.State.ITransition> RunSubject(
            IEnumerable<PowerFull.Device.State> deviceStates,
            TimeSpan runFor,
            PowerFull.Service.Config config = null,
            Func<TestScheduler, ITestableObservable<(Event, IDevice)>> solicitedPowerChanges = null,
            Func<TestScheduler, ITestableObservable<(IDevice, PowerState)>> unsolicitedPowerChanges = null)
        {
            var scheduler = new TestScheduler();

            solicitedPowerChanges = solicitedPowerChanges ?? NullFactory<Event, IDevice>;
            unsolicitedPowerChanges = unsolicitedPowerChanges ?? NullFactory<IDevice, PowerState>;

            config = config ?? new PowerFull.Service.Config();

            var transitionFactory = A.Fake<PowerFull.Service.State.Transition.IFactory>();
            A.CallTo(() => transitionFactory.ToInitializing(A<PowerFull.Service.State.IPayload>.Ignored)).Returns(TransitionToInitializing);
            A.CallTo(() => transitionFactory.ToRunning(A<PowerFull.Service.State.IPayload>.Ignored)).Returns(TransitionToRunning);
            var logic = A.Fake<PowerFull.Service.State.ILogic>();
            A.CallTo(() => logic.GenerateEvents(A<IObservable<double>>.Ignored, A<IEnumerable<PowerFull.Device.State>>.Ignored))
             .Returns(solicitedPowerChanges(scheduler));
            var payload = A.Fake<PowerFull.Service.State.IPayload>();
            A.CallTo(() => payload.Devices).Returns(deviceStates);
            var messagingFacade = A.Fake<PowerFull.Messaging.IFacade>();
            A.CallTo(() => messagingFacade.PowerStateChanges(A<IEnumerable<PowerFull.IDevice>>.Ignored))
             .Returns(unsolicitedPowerChanges(scheduler));
            A.CallTo(() => payload.MessagingFacade).Returns(messagingFacade);

            var subject = new PowerFull.Service.State.Running(transitionFactory, logic, payload, config, scheduler);
            return scheduler.Start(() => subject.Enter(), runFor.AsTestDisposalTime());
        }

        [Test]
        public void EmitATransitionToRunningWhenAPowerEventTakesPlace()
        {
            var deviceStates = new [] { 
                new PowerFull.Device.State(DeviceA, 0, PowerState.On), 
                new PowerFull.Device.State(DeviceB, 1, PowerState.On) 
            };

            var actual = RunSubject(
                deviceStates,
                TimeSpan.FromMinutes(10),
                solicitedPowerChanges: scheduler => scheduler.CreateColdObservable(Recorded.OnNext(TimeSpan.FromMinutes(10), (Event.TurnOff, DeviceB)))
            );

            var transitionsToRunning = actual.Messages
                .Count(recorded => recorded.Value.Kind == NotificationKind.OnNext && recorded.Value.Value == TransitionToRunning);

            Assert.That(transitionsToRunning, Is.EqualTo(1));
        }

        [Test]
        public void EmitATransitionToRunningWhenAnUnsolicitedPowerEventTakesPlace()
        {
            var deviceStates = new[] {
                new PowerFull.Device.State(DeviceA, 0, PowerState.On),
                new PowerFull.Device.State(DeviceB, 1, PowerState.On)
            };

            var actual = RunSubject(
                deviceStates,
                TimeSpan.FromMinutes(10),
                unsolicitedPowerChanges: scheduler => scheduler.CreateColdObservable(Recorded.OnNext(TimeSpan.FromMinutes(10), (DeviceB, PowerState.Off)))
            );

            var transitionsToRunning = actual.Messages
                .Count(recorded => recorded.Value.Kind == NotificationKind.OnNext && recorded.Value.Value == TransitionToRunning);

            Assert.That(transitionsToRunning, Is.EqualTo(1));
        }


        [Test]
        public void EmitATransitionToInitializingWhenAPowerEventHasNotOccuredInTheConfiguredPeriod()
        {
            var deviceStates = new [] 
            {
                new PowerFull.Device.State(DeviceA, 0, PowerState.On),
                new PowerFull.Device.State(DeviceB, 1, PowerState.On) 
            };

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
