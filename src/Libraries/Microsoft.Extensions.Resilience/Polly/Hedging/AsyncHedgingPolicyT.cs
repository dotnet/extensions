// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Options;
using Polly;

namespace Microsoft.Extensions.Resilience.Hedging;

#pragma warning disable SA1649 // File name should match first type name

/// <summary>
/// A hedging policy that can be applied to delegates.
/// </summary>
/// <typeparam name="TResult">
/// The return type of delegates which may be executed through the policy.
/// </typeparam>
internal sealed class AsyncHedgingPolicy<TResult> : AsyncPolicy<TResult>, IsPolicy
{
    private readonly HedgingEngineOptions<TResult> _hedgingEngineOptions;
    private readonly HedgedTaskProvider<TResult> _hedgedTaskProvider;

    internal AsyncHedgingPolicy(
        PolicyBuilder<TResult> policyBuilder,
        HedgedTaskProvider<TResult> hedgedTaskProvider,
        int maxHedgedTasks,
        Func<HedgingDelayArguments, TimeSpan> hedgingDelayGenerator,
        Func<DelegateResult<TResult>, Context, int, CancellationToken, Task> onHedgingAsync)
        : base(policyBuilder)
    {
        _hedgedTaskProvider = hedgedTaskProvider;
        _hedgingEngineOptions = new HedgingEngineOptions<TResult>(
            maxHedgedTasks,
            hedgingDelayGenerator,
            ExceptionPredicates,
            ResultPredicates,
            onHedgingAsync);
    }

    /// <inheritdoc/>
    protected override Task<TResult> ImplementationAsync(
        Func<Context, CancellationToken, Task<TResult>> action,
        Context context,
        CancellationToken cancellationToken,
        bool continueOnCapturedContext)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return HedgingEngine<TResult>.ExecuteAsync(
            action,
            context,
            _hedgedTaskProvider,
            _hedgingEngineOptions,
            continueOnCapturedContext,
            cancellationToken);
    }
}
