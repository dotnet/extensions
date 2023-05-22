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
#pragma warning disable CA1815 // Override equals and operator equals on value types (Such usage is not expected in this scenario)
public readonly struct BreakActionArguments : IPolicyEventArguments
#pragma warning restore CA1815
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BreakActionArguments" /> structure.
    /// </summary>
    /// <param name="exception">The fault.</param>
    /// <param name="context">The policy context.</param>
    /// <param name="breakDuration">The duration of break.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public BreakActionArguments(
        Exception exception,
        Context context,
        TimeSpan breakDuration,
        CancellationToken cancellationToken)
    {
        _ = Throw.IfLessThanOrEqual(breakDuration.Ticks, 0);
        BreakDuration = breakDuration;
        Exception = Throw.IfNull(exception);
        Context = Throw.IfNull(context);
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the result of the action executed by the retry policy.
    /// </summary>
    public Exception Exception { get; }

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
