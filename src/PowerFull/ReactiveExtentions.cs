using Bebbs.Monads;
using System;
using System.Reactive.Linq;

namespace PowerFull
{
    public static class ReactiveExtentions
    {
        public static IObservable<T> Collect<T>(this IObservable<Option<T>> source)
        {
            return source.Where(item => item.IsSome).Select(item => item.Value);
        }
    }
}
