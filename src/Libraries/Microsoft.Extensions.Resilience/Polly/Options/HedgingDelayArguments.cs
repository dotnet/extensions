// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Resilience.Options;

/// <summary>
/// Structure with the arguments used by <see cref="HedgingPolicyOptions.HedgingDelayGenerator"/>.
/// </summary>
#pragma warning disable CA1815 // Override equals and operator equals on value types (Such usage is not expected in this scenario)
public readonly struct HedgingDelayArguments : IPolicyEventArguments
#pragma warning restore CA1815
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HedgingDelayArguments"/> structure.
    /// </summary>
    /// <param name="context">The policy context.</param>
    /// <param name="attemptNumber">The attempt number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public HedgingDelayArguments(Context context, int attemptNumber, CancellationToken cancellationToken)
    {
        Context = Throw.IfNull(context);
        AttemptNumber = Throw.IfLessThan(attemptNumber, 0);
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the zero-based hedging attempt number.
    /// </summary>
    public int AttemptNumber { get; }

    /// <summary>
    /// Gets the Polly <see cref="global::Polly.Context" /> associated with the policy execution.
    /// </summary>
    public Context Context { get; }

    /// <summary>
    /// Gets the cancellation token associated with the policy execution.
    /// </summary>
    public CancellationToken CancellationToken { get; }
}
