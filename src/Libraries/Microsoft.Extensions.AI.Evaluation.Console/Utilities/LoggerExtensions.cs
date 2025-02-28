// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable EA0014
// EA0014: Async methods should support cancellation.
// We disable this warning because the helpers in this file are wrapper functions that don't themselves perform any
// cancellable operations.

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
        Action action,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            action();
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

    internal static void ExecuteWithCatch<TArgument>(
        this ILogger logger,
        Action<TArgument> action,
        TArgument argument,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            action(argument);
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
        Func<TResult> action,
        TResult? defaultValue = default,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            return action();
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

    internal static TResult? ExecuteWithCatch<TArgument, TResult>(
        this ILogger logger,
        Func<TArgument, TResult> action,
        TArgument argument,
        TResult? defaultValue = default,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            return action(argument);
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

    internal static async Task ExecuteWithCatchAsync(
        this ILogger logger,
        Func<Task> action,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            await action().ConfigureAwait(false);
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
        Func<ValueTask> action,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            await action().ConfigureAwait(false);
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

    internal static async Task ExecuteWithCatchAsync<TArgument>(
        this ILogger logger,
        Func<TArgument, Task> action,
        TArgument argument,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            await action(argument).ConfigureAwait(false);
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

    internal static async ValueTask ExecuteWithCatchAsync<TArgument>(
        this ILogger logger,
        Func<TArgument, ValueTask> action,
        TArgument argument,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            await action(argument).ConfigureAwait(false);
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

    internal static async Task<TResult?> ExecuteWithCatchAsync<TResult>(
        this ILogger logger,
        Func<Task<TResult>> action,
        TResult? defaultValue = default,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            return await action().ConfigureAwait(false);
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
        Func<ValueTask<TResult>> action,
        TResult? defaultValue = default,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            return await action().ConfigureAwait(false);
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

    internal static async Task<TResult?> ExecuteWithCatchAsync<TArgument, TResult>(
        this ILogger logger,
        Func<TArgument, Task<TResult>> action,
        TArgument argument,
        TResult? defaultValue = default,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            return await action(argument).ConfigureAwait(false);
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

    internal static async ValueTask<TResult?> ExecuteWithCatchAsync<TArgument, TResult>(
        this ILogger logger,
        Func<TArgument, ValueTask<TResult>> action,
        TArgument argument,
        TResult? defaultValue = default,
        bool swallowUnhandledExceptions = false)
    {
        try
        {
            return await action(argument).ConfigureAwait(false);
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
}
