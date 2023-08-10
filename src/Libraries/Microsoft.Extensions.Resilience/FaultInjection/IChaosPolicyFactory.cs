// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
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
    /// A latency policy.
    /// </returns>
    public AsyncInjectLatencyPolicy<TResult> CreateLatencyPolicy<TResult>();

    /// <summary>
    /// Creates an async exception policy with delegate functions to fetch
    /// fault injection settings from <see cref="Context"/>.
    /// </summary>
    /// <returns>
    /// An exception policy.
    /// </returns>
    public AsyncInjectOutcomePolicy CreateExceptionPolicy();

    /// <summary>
    /// Creates an async custom result policy with delegate functions to fetch
    /// fault injection settings from <see cref="Context"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of value policies created by this method will inject.</typeparam>
    /// <returns>A custom result policy.</returns>
    [Experimental(diagnosticId: Experiments.Resilience, UrlFormat = Experiments.UrlFormat)]
    public AsyncInjectOutcomePolicy<TResult> CreateCustomResultPolicy<TResult>();
}
