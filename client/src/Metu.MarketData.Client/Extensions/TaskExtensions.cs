using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Metu.MarketData.Client.Extensions;

public static class TaskExtensions
{
    public static Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> body)
    {
        var exceptions = new ConcurrentBag<Exception>();

        void ObserveException(Task task)
        {
            if (task.Exception != null)
            {
                exceptions.Add(task.Exception);
            }
        }

        void RaiseExceptions(Task _)
        {
            if (exceptions.Any())
            {
                throw (exceptions.Count == 1
                    ? exceptions.Single()
                    : new AggregateException(exceptions).Flatten());
            }
        }

        return Task.WhenAll(
            from item in source
            select Task.Run
            (
                () => body(item).ContinueWith(ObserveException)
            )
            .ContinueWith(ObserveException)
            .ContinueWith(RaiseExceptions));
    }
    public static Task ForEachAsync<T>(this IEnumerable<T> source, int degreeOfParallelism, Func<T, Task> body, CancellationToken cancellationToken)
    {
        var exceptions = new ConcurrentBag<Exception>();

        void ObserveException(Task task)
        {
            if (task.Exception != null)
            {
                exceptions.Add(task.Exception);
            }
        }

        void RaiseExceptions(Task _)
        {
            if (exceptions.Any())
            {
                throw (exceptions.Count == 1
                    ? exceptions.Single()
                    : new AggregateException(exceptions).Flatten());
            }
        }

        return Task.WhenAll
        (
            from partition in Partitioner.Create(source).GetPartitions(degreeOfParallelism)
            select Task.Run(async delegate
            {
                using (partition)
                    while ((cancellationToken == default || !cancellationToken.IsCancellationRequested) && partition.MoveNext())
                        await body(partition.Current).ContinueWith(ObserveException);
            })
            .ContinueWith(ObserveException)
            .ContinueWith(RaiseExceptions)
        );
    }
}