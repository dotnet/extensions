// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.AI.Evaluation.Console.Utilities;

internal static class LoggerExtensions
{
    internal static bool LogException(this ILogger logger, Exception exception)
    {
        logger.LogError(exception, message: null);
        return true;
    }

    internal static void ExecuteWithCatch(
        this ILogger logger,
        Action operation,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            operation();
        }
        catch (Exception ex) when (swallowUnhandledExceptions && ex.IsCancellation())
        {
            // Do nothing.
        }
        catch (Exception ex) when (!ex.IsCancellation() && logger.LogException(ex) && swallowUnhandledExceptions)
        {
            // Do nothing. The exception is logged in the when clause above.
        }
    }

    internal static TResult? ExecuteWithCatch<TResult>(
        this ILogger logger,
        Func<TResult> operation,
        TResult? defaultValue = default,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            return operation();
        }
        catch (Exception ex) when (swallowUnhandledExceptions && ex.IsCancellation())
        {
            // Do nothing.
        }
        catch (Exception ex) when (!ex.IsCancellation() && logger.LogException(ex) && swallowUnhandledExceptions)
        {
            // Do nothing. The exception is logged in the when clause above.
        }

        return defaultValue;
    }

#pragma warning disable EA0014 // The async method doesn't support cancellation
    internal static async ValueTask ExecuteWithCatchAsync(
        this ILogger logger,
        Func<Task> operation,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            await operation().ConfigureAwait(false);
        }
        catch (Exception ex) when (swallowUnhandledExceptions && ex.IsCancellation())
        {
            // Do nothing.
        }
        catch (Exception ex) when (!ex.IsCancellation() && logger.LogException(ex) && swallowUnhandledExceptions)
        {
            // Do nothing. The exception is logged in the when clause above.
        }
    }

    internal static async ValueTask ExecuteWithCatchAsync(
        this ILogger logger,
        Func<ValueTask> operation,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            await operation().ConfigureAwait(false);
        }
        catch (Exception ex) when (swallowUnhandledExceptions && ex.IsCancellation())
        {
            // Do nothing.
        }
        catch (Exception ex) when (!ex.IsCancellation() && logger.LogException(ex) && swallowUnhandledExceptions)
        {
            // Do nothing. The exception is logged in the when clause above.
        }
    }

    internal static async ValueTask<TResult?> ExecuteWithCatchAsync<TResult>(
        this ILogger logger,
        Func<Task<TResult>> operation,
        TResult? defaultValue = default,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (Exception ex) when (swallowUnhandledExceptions && ex.IsCancellation())
        {
            // Do nothing.
        }
        catch (Exception ex) when (!ex.IsCancellation() && logger.LogException(ex) && swallowUnhandledExceptions)
        {
            // Do nothing. The exception is logged in the when clause above.
        }

        return defaultValue;
    }

    internal static async ValueTask<TResult?> ExecuteWithCatchAsync<TResult>(
        this ILogger logger,
        Func<ValueTask<TResult>> operation,
        TResult? defaultValue = default,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (Exception ex) when (swallowUnhandledExceptions && ex.IsCancellation())
        {
            // Do nothing.
        }
        catch (Exception ex) when (!ex.IsCancellation() && logger.LogException(ex) && swallowUnhandledExceptions)
        {
            // Do nothing. The exception is logged in the when clause above.
        }

        return defaultValue;
    }
#pragma warning restore EA0014
}
