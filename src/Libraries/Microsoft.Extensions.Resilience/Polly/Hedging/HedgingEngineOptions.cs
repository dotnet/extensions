// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Options;
using Polly;

namespace Microsoft.Extensions.Resilience.Hedging;

internal sealed class HedgingEngineOptions<TResult>
{
    public int MaxHedgedTasks { get; }

    public Func<HedgingDelayArguments, TimeSpan> HedgingDelayGenerator { get; }

    public ExceptionPredicates ShouldHandleExceptionPredicates { get; }

    public ResultPredicates<TResult> ShouldHandleResultPredicates { get; }

    public Func<DelegateResult<TResult>, Context, int, CancellationToken, Task> OnHedgingAsync { get; }

    public HedgingEngineOptions(
        int maxHedgedTasks,
        Func<HedgingDelayArguments, TimeSpan> hedgingDelayGenerator,
        ExceptionPredicates shouldHandleExceptionPredicates,
        ResultPredicates<TResult> shouldHandleResultPredicates,
        Func<DelegateResult<TResult>, Context, int, CancellationToken, Task> onHedgingAsync)
    {
        MaxHedgedTasks = maxHedgedTasks;
        ShouldHandleExceptionPredicates = shouldHandleExceptionPredicates;
        ShouldHandleResultPredicates = shouldHandleResultPredicates;
        OnHedgingAsync = onHedgingAsync;
        HedgingDelayGenerator = hedgingDelayGenerator;
    }
}
