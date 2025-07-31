// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Utilities;

internal static class TimingHelper
{
    internal static TimeSpan ExecuteWithTiming(Action operation)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            operation();
        }
        finally
        {
            stopwatch.Stop();
        }

        return stopwatch.Elapsed;
    }

    internal static (TResult result, TimeSpan duration) ExecuteWithTiming<TResult>(Func<TResult> operation)
    {
        TResult result;
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            result = operation();
        }
        finally
        {
            stopwatch.Stop();
        }

        return (result, duration: stopwatch.Elapsed);
    }

#pragma warning disable EA0014 // The async method doesn't support cancellation
    internal static async ValueTask<TimeSpan> ExecuteWithTimingAsync(Func<Task> operation)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            await operation().ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();
        }

        return stopwatch.Elapsed;
    }

    internal static async ValueTask<TimeSpan> ExecuteWithTimingAsync(Func<ValueTask> operation)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            await operation().ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();
        }

        return stopwatch.Elapsed;
    }

    internal static async ValueTask<(TResult result, TimeSpan duration)> ExecuteWithTimingAsync<TResult>(
        Func<Task<TResult>> operation)
    {
        TResult result;
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            result = await operation().ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();
        }

        return (result, duration: stopwatch.Elapsed);
    }

    internal static async ValueTask<(TResult result, TimeSpan duration)> ExecuteWithTimingAsync<TResult>(
        Func<ValueTask<TResult>> operation)
    {
        TResult result;
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            result = await operation().ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();
        }

        return (result, duration: stopwatch.Elapsed);
    }
#pragma warning restore EA0014
}
