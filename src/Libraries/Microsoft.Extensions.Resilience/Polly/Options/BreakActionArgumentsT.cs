// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Resilience.Options;

/// <summary>
/// Structure with the arguments of the on break action.
/// </summary>
/// <typeparam name="TResult">The type of the result handled by the policy.</typeparam>
#pragma warning disable CA1815 // Override equals and operator equals on value types (Such usage is not expected in this scenario)
#pragma warning disable SA1649 // File name should match first type name
public readonly struct BreakActionArguments<TResult> : IPolicyEventArguments<TResult>
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore CA1815
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BreakActionArguments{TResult}" /> structure.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="context">The policy context.</param>
    /// <param name="breakDuration">The duration of break.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public BreakActionArguments(
        DelegateResult<TResult> result,
        Context context,
        TimeSpan breakDuration,
        CancellationToken cancellationToken)
    {
        _ = Throw.IfLessThanOrEqual(breakDuration.Ticks, 0);
        BreakDuration = breakDuration;
        Result = Throw.IfNull(result);
        Context = Throw.IfNull(context);
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the result of the action executed by the retry policy.
    /// </summary>
    public DelegateResult<TResult> Result { get; }

    /// <summary>
    /// Gets the duration of break.
    /// </summary>
    public TimeSpan BreakDuration { get; }

    /// <summary>
    /// Gets the Polly <see cref="global::Polly.Context" /> associated with the policy execution.
    /// </summary>
    public Context Context { get; }

    /// <summary>
    /// Gets the cancellation token associated with the policy execution.
    /// </summary>
    public CancellationToken CancellationToken { get; }
}
