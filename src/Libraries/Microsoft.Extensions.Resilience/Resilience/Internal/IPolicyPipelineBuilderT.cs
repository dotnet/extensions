// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Resilience.Options;
using Polly;

namespace Microsoft.Extensions.Resilience.Internal;

#pragma warning disable SA1649 // File name should match first type name

/// <summary>
/// Chains policies, returning a policy wrapper to be registered under a unique key.
/// </summary>
/// <typeparam name="TResult">The type of the result returned by the action executed by the policies.</typeparam>
internal interface IPolicyPipelineBuilder<TResult>
{
    /// <summary>
    /// Sets the pipeline identifiers.
    /// </summary>
    /// <param name="pipelineId">The pipeline id.</param>
    void Initialize(PipelineId pipelineId);

    /// <summary>
    /// Adds a circuit breaker policy.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="options">The <see cref="CircuitBreakerPolicyOptions{TResult}"/> containing the options for the circuit breaker.</param>
    /// <returns>
    /// Current instance.
    /// </returns>
    IPolicyPipelineBuilder<TResult> AddCircuitBreakerPolicy(
        string policyName,
        CircuitBreakerPolicyOptions<TResult> options);

    /// <summary>
    /// Adds a retry policy.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="options">The <see cref="RetryPolicyOptions{TResult}"/> containing the options for the retry policy.</param>
    /// <returns>
    /// Current instance.
    /// </returns>
    IPolicyPipelineBuilder<TResult> AddRetryPolicy(
        string policyName,
        RetryPolicyOptions<TResult> options);

    /// <summary>
    /// Adds a timeout policy.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="options">The options of the policy.</param>
    /// <returns>
    /// Current instance.
    /// </returns>
    IPolicyPipelineBuilder<TResult> AddTimeoutPolicy(
        string policyName,
        TimeoutPolicyOptions options);

    /// <summary>
    /// Adds a fallback policy.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="provider">The task performed in the fallback scenario when the initial execution encounters a transient failure.</param>
    /// <param name="options">The options of the fallback policy.</param>
    /// <returns>
    /// Current instance.
    /// </returns>
    IPolicyPipelineBuilder<TResult> AddFallbackPolicy(
        string policyName,
        FallbackScenarioTaskProvider<TResult> provider,
        FallbackPolicyOptions<TResult> options);

    /// <summary>
    /// Adds a bulkhead policy.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="options">The options of the policy.</param>
    /// <returns>
    /// Current instance.
    /// </returns>
    IPolicyPipelineBuilder<TResult> AddBulkheadPolicy(
        string policyName,
        BulkheadPolicyOptions options);

    /// <summary>
    /// Adds the hedging policy.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="provider">The hedged task provider.</param>
    /// <param name="options">The options.</param>
    /// <returns>Current instance.</returns>
    IPolicyPipelineBuilder<TResult> AddHedgingPolicy(
        string policyName,
        HedgedTaskProvider<TResult> provider,
        HedgingPolicyOptions<TResult> options);

    /// <summary>
    /// Adds a custom policy to a pipeline.
    /// </summary>
    /// <param name="policy">The policy instance.</param>
    /// <returns>Current instance.</returns>
    IPolicyPipelineBuilder<TResult> AddPolicy(IAsyncPolicy<TResult> policy);

    /// <summary>
    /// Adds a custom policy to a pipeline.
    /// </summary>
    /// <param name="policy">The policy instance.</param>
    /// <returns>Current instance.</returns>
    IPolicyPipelineBuilder<TResult> AddPolicy(IAsyncPolicy policy);

    /// <summary>
    /// Builds an <see cref="AsyncPolicy{TResult}" /> instance.
    /// </summary>
    /// <returns>The policy wrap containing all chained policies.</returns>
    public IAsyncPolicy<TResult> Build();
}
