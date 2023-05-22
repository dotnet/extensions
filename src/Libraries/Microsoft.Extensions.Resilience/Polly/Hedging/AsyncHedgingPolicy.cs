// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Options;
using Polly;

namespace Microsoft.Extensions.Resilience.Hedging;

/// <summary>
/// A hedging policy that can be applied to delegates.
/// </summary>
/// The return type of delegates which may be executed through the policy.
internal sealed class AsyncHedgingPolicy : AsyncPolicy, IsPolicy
{
    private readonly HedgingEngineOptions<EmptyStruct> _hedgingEngineOptions;
    private readonly HedgedTaskProvider<EmptyStruct> _hedgedTaskProvider;

    internal AsyncHedgingPolicy(
        PolicyBuilder policyBuilder,
        HedgedTaskProvider<EmptyStruct> hedgedTaskProvider,
        int maxHedgedTasks,
        Func<HedgingDelayArguments, TimeSpan> hedgingDelayGenerator,
        Func<DelegateResult<EmptyStruct>, Context, int, CancellationToken, Task> onHedgingAsync)
        : base(policyBuilder)
    {
        _hedgedTaskProvider = hedgedTaskProvider;
        _hedgingEngineOptions = new HedgingEngineOptions<EmptyStruct>(
            maxHedgedTasks,
            hedgingDelayGenerator,
            ExceptionPredicates,
            ResultPredicates<EmptyStruct>.None,
            onHedgingAsync);
    }

    protected override async Task<TResult> ImplementationAsync<TResult>(
        Func<Context, CancellationToken, Task<TResult>> action,
        Context context,
        CancellationToken cancellationToken,
        bool continueOnCapturedContext)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TResult? result = default;

        _ = await HedgingEngine<EmptyStruct>.ExecuteAsync(
            async (ctx, ct) =>
            {
                result = await action(ctx, ct).ConfigureAwait(continueOnCapturedContext);
                return EmptyStruct.Instance;
            },
            context,
            _hedgedTaskProvider,
            _hedgingEngineOptions,
            continueOnCapturedContext,
            cancellationToken).ConfigureAwait(continueOnCapturedContext);

#pragma warning disable CS8603 // Possible null reference return.
        return result;
#pragma warning restore CS8603 // Possible null reference return.
    }
}
