// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Options;
using Polly;

namespace Microsoft.Extensions.Resilience.Hedging;

/// <summary>
/// Fluent API for defining a hedging <see cref="AsyncPolicy"/>.
/// </summary>
internal static class AsyncHedgingSyntax
{
    /// <summary>
    /// Builds an <see cref="Hedging.AsyncHedgingPolicy{TResult}" /> which provides the fastest result
    /// returned from a set of tasks (i.e. hedged execution) if the main execution fails or is too slow.
    /// If this throws a handled exception or raises a handled result,
    /// if asynchronously calls <paramref name="onHedgingAsync" />
    /// with details of the handled exception or result and the execution context;
    /// Then will continue to wait and check for the first allowed of task provided by
    /// the <paramref name="hedgedTaskProvider" /> and returns its result;
    /// If none of the tasks returned by <paramref name="hedgedTaskProvider" /> returns an allowed result,
    /// the last handled exception or result will be returned.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="policyBuilder">The policy builder.</param>
    /// <param name="hedgedTaskProvider">The hedged action provider.</param>
    /// <param name="maxHedgedTasks">The maximum hedged tasks.</param>
    /// <param name="hedgingDelayGenerator">The delegate that provides the hedging delay for each hedged task.</param>
    /// <param name="onHedgingAsync">The action to call asynchronously after invoking one hedged task.</param>
    /// <returns>
    /// The policy instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">Any of the parameters are <see langword="null"/>.</exception>
    public static AsyncHedgingPolicy<TResult> AsyncHedgingPolicy<TResult>(
        this PolicyBuilder<TResult> policyBuilder,
        HedgedTaskProvider<TResult> hedgedTaskProvider,
        int maxHedgedTasks,
        Func<HedgingDelayArguments, TimeSpan> hedgingDelayGenerator,
        Func<DelegateResult<TResult>, Context, int, CancellationToken, Task> onHedgingAsync)
    {
        return new AsyncHedgingPolicy<TResult>(
                 policyBuilder,
                 hedgedTaskProvider,
                 maxHedgedTasks,
                 hedgingDelayGenerator,
                 onHedgingAsync);
    }

    public static AsyncHedgingPolicy AsyncHedgingPolicy(
        this PolicyBuilder policyBuilder,
        HedgedTaskProvider hedgedTaskProvider,
        int maxHedgedTasks,
        Func<HedgingDelayArguments, TimeSpan> hedgingDelayGenerator,
        Func<Exception, Context, int, CancellationToken, Task> onHedgingAsync)
    {
        return new AsyncHedgingPolicy(
                 policyBuilder,
                 WrapProvider(hedgedTaskProvider),
                 maxHedgedTasks,
                 hedgingDelayGenerator,
                 (ex, ctx, task, token) => onHedgingAsync(ex.Exception, ctx, task, token));
    }

    internal static HedgedTaskProvider<EmptyStruct> WrapProvider(HedgedTaskProvider provider)
    {
        return WrappedProvider;

        bool WrappedProvider(HedgingTaskProviderArguments args, [NotNullWhen(true)] out Task<EmptyStruct>? result)
        {
            if (provider(args, out var hedgingTask) && hedgingTask is not null)
            {
                result = hedgingTask.ContinueWith(task => EmptyStruct.Instance, TaskScheduler.Default);
                return true;
            }

            result = null;
            return false;
        }
    }
}
