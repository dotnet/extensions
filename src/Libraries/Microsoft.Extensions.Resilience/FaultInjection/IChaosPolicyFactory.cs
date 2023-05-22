// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Polly;
using Polly.Contrib.Simmy.Latency;
using Polly.Contrib.Simmy.Outcomes;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Factory for chaos policy creation.
/// </summary>
public interface IChaosPolicyFactory
{
    /// <summary>
    /// Creates an async latency policy with delegate functions to fetch fault injection
    /// settings from <see cref="Context"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of value policies created by this method will inject.</typeparam>
    /// <returns>
    /// A latency policy,
    /// an instance of <see cref="AsyncInjectLatencyPolicy{TResult}"/>.
    /// </returns>
    public AsyncInjectLatencyPolicy<TResult> CreateLatencyPolicy<TResult>();

    /// <summary>
    /// Creates an async exception policy with delegate functions to fetch
    /// fault injection settings from <see cref="Context"/>.
    /// </summary>
    /// <returns>
    /// An exception policy,
    /// an instance of <see cref="AsyncInjectOutcomePolicy"/>.
    /// </returns>
    public AsyncInjectOutcomePolicy CreateExceptionPolicy();
}
