// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Utilities;

internal static class TaskExtensions
{
    internal static IAsyncEnumerable<T> ExecuteConcurrentlyAndStreamResultsAsync<T>(
        this IEnumerable<Func<CancellationToken, Task<T>>> functions,
        bool preserveOrder = false,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Task<T>> concurrentTasks = functions.Select(f => f(cancellationToken));
        return concurrentTasks.StreamResultsAsync(preserveOrder, cancellationToken);
    }

    internal static IAsyncEnumerable<T> ExecuteConcurrentlyAndStreamResultsAsync<T>(
        this IEnumerable<Func<CancellationToken, ValueTask<T>>> functions,
        bool preserveOrder = false,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<ValueTask<T>> concurrentTasks = functions.Select(f => f(cancellationToken));
        return concurrentTasks.StreamResultsAsync(preserveOrder, cancellationToken);
    }

    /// <remarks>
    /// <para>
    /// This method assumes that all the tasks supplied via <paramref name="concurrentTasks"/> are already running.
    /// </para>
    /// <para>
    /// Ideally, the <see cref="CancellationToken"/> passed via <paramref name="cancellationToken"/> should also cancel
    /// the tasks supplied via <paramref name="concurrentTasks"/>.
    /// </para>
    /// </remarks>
    internal static async IAsyncEnumerable<T> StreamResultsAsync<T>(
        this IEnumerable<Task<T>> concurrentTasks,
        bool preserveOrder = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (preserveOrder)
        {
            foreach (Task<T> task in concurrentTasks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return await task.ConfigureAwait(false);
            }
        }
        else
        {
#if NET9_0_OR_GREATER
            await foreach (Task<T> task in
                Task.WhenEach(concurrentTasks).WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return await task.ConfigureAwait(false);
            }
#else
            var remaining = new HashSet<Task<T>>(concurrentTasks);

            while (remaining.Count is not 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var task = await Task.WhenAny(remaining).ConfigureAwait(false);
                _ = remaining.Remove(task);
                yield return await task.ConfigureAwait(false);
            }
#endif
        }
    }

    internal static async IAsyncEnumerable<T> StreamResultsAsync<T>(
        this IEnumerable<ValueTask<T>> concurrentTasks,
        bool preserveOrder = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (preserveOrder)
        {
            foreach (ValueTask<T> task in concurrentTasks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return await task.ConfigureAwait(false);
            }
        }
        else
        {
            IAsyncEnumerable<T> results =
                StreamResultsAsync(concurrentTasks.Select(t => t.AsTask()), preserveOrder, cancellationToken);

            await foreach (T result in results.ConfigureAwait(false))
            {
                yield return result;
            }
        }
    }
}
