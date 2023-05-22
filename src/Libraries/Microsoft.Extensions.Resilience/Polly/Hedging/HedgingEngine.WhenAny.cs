// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable R9A061

namespace Microsoft.Extensions.Resilience.Hedging;

internal static partial class HedgingEngine<TResult>
{
    private static Task<Task<TResult>> WhenAnyAsync(Dictionary<Task<TResult>, CancellationPair>.KeyCollection tasks)
    {
#pragma warning disable S109 // Magic numbers should not be used
        return tasks.Count switch
        {
            1 => WhenAny1Async(tasks),
            2 => WhenAny2Async(tasks),
            _ => Task.WhenAny(tasks)
        };
#pragma warning restore S109 // Magic numbers should not be used

        static async Task<Task<TResult>> WhenAny1Async(Dictionary<Task<TResult>, CancellationPair>.KeyCollection tasks)
        {
            using var enumerator = tasks.GetEnumerator();
            _ = enumerator.MoveNext();

            try
            {
                _ = await enumerator.Current.ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                // discard exception and propagate it in task
            }

            return enumerator.Current;
        }

        static Task<Task<TResult>> WhenAny2Async(Dictionary<Task<TResult>, CancellationPair>.KeyCollection tasks)
        {
            using var enumerator = tasks.GetEnumerator();

            _ = enumerator.MoveNext();
            var first = enumerator.Current;

            _ = enumerator.MoveNext();
            var second = enumerator.Current;

            return Task.WhenAny(first, second);
        }
    }
}
