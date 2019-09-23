using Microsoft.Reactive.Testing;
using System;

namespace PowerFull.Tests
{
    public static class Helpers
    {
        public static readonly TimeSpan ReactiveTestCreated = TimeSpan.FromTicks(ReactiveTest.Created);
        public static readonly TimeSpan ReactiveTestSubscribed = TimeSpan.FromTicks(ReactiveTest.Subscribed);
        public static readonly TimeSpan One = TimeSpan.FromTicks(1);

        public static TimeSpan AsTestTime(this TimeSpan timeSpan)
        {
            return timeSpan.Add(ReactiveTestSubscribed);
        }

        public static TimeSpan AsTestDisposalTime(this TimeSpan timeSpan)
        {
            return timeSpan.AsTestTime().Add(One);
        }

        public static ITestableObserver<T> Start<T>(this TestScheduler scheduler, Func<IObservable<T>> create, TimeSpan disposed)
        {
            var disposal = disposed.AsTestDisposalTime().Ticks;

            return scheduler.Start(create, disposal);
        }
    }
}
