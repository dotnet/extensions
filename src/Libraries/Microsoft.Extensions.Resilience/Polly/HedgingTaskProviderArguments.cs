// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Resilience;

/// <summary>
/// A wrapper that holds current request's <see cref="global::Polly.Context"/>
/// and the current hedging attempt number.
/// </summary>
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Comparing instances is not an expected scenario")]
public readonly struct HedgingTaskProviderArguments : IPolicyEventArguments
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HedgingTaskProviderArguments"/> struct.
    /// </summary>
    /// <param name="context">Current request's context.</param>
    /// <param name="attemptNumber">Count of already executed hedging attempts.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public HedgingTaskProviderArguments(Context context, int attemptNumber, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(context);

        Context = context;
        AttemptNumber = attemptNumber;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the hedging attempt number.
    /// </summary>
    /// <remarks>The attempt number starts with the 1 as <see cref="HedgingDelayArguments"/> is used after the primary hedging attempt is executed.</remarks>
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
