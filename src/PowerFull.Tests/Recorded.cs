using Microsoft.Reactive.Testing;
using System;
using System.Reactive;

namespace PowerFull.Tests
{
    public static class Recorded
    {
        public static Recorded<Notification<T>> OnNext<T>(TimeSpan timeSpan, T value)
        {
            return new Recorded<Notification<T>>(timeSpan.Ticks, Notification.CreateOnNext(value));
        }

        public static Recorded<Notification<T>> ExpectedOnNext<T>(TimeSpan timeSpan, T value)
        {
            return new Recorded<Notification<T>>(timeSpan.AsTestTime().Ticks, Notification.CreateOnNext(value));
        }

        public static Recorded<Notification<T>> OnCompleted<T>(TimeSpan timeSpan)
        {
            return new Recorded<Notification<T>>(timeSpan.Ticks, Notification.CreateOnCompleted<T>());
        }
    }
}
