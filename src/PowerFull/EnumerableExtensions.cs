using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PowerFull
{
    public static class EnumerableExtensions
    {
        public static async IAsyncEnumerable<TResult> SelectAsync<TSource,TResult>(this IEnumerable<TSource> source, Func<TSource, Task<TResult>> projection)
        {
            foreach (TSource item in source)
            {
                var result = await projection(item);

                yield return result;
            }
        }

        public static async Task<T[]> ToArray<T>(this IAsyncEnumerable<T> source)
        {
            List<T> result = new List<T>();

            await foreach (T item in source)
            {
                result.Add(item);
            }

            return result.ToArray();
        }
    }
}
