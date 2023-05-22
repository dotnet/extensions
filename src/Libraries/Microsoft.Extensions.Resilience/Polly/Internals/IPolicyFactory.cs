// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Resilience.Options;
using Polly;

namespace Microsoft.Extensions.Resilience.Internal;

/// <summary>
/// Factory interface for policy creation.
/// </summary>
internal interface IPolicyFactory
{
    /// <summary>
    /// Sets the pipeline identifiers.
    /// </summary>
    /// <param name="pipelineId">The pipeline id.</param>
    void Initialize(PipelineId pipelineId);

    /// <summary>
    /// Creates an advanced circuit breaker policy which allow granular customization of the fault tolerance.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="options">The configuration.</param>
    /// <returns>A circuit breaker policy.</returns>
    /// <remarks>
    /// Reacts on proportion of failures (i.e. failureThreshold) by measuring the data within a custom interval (i.e. sampling duration)
    /// Imposes a minimal time interval before acting (i.e. minimumThroughput) and configurable break duration.
    /// <seealso href="http://github.com/App-vNext/Polly/wiki/Advanced-Circuit-Breaker" />
    /// </remarks>
    IAsyncPolicy CreateCircuitBreakerPolicy(string policyName, CircuitBreakerPolicyOptions options);

    /// <summary>
    /// Creates an advanced circuit breaker policy which allow granular customization of the fault tolerance.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="options">The configuration.</param>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policy.</typeparam>
    /// <returns>A circuit breaker policy.</returns>
    /// <remarks>
    /// Reacts on proportion of failures (i.e. failureThreshold) by measuring the data within a custom interval (i.e. sampling duration)
    /// Imposes a minimal time interval before acting (i.e. minimumThroughput) and configurable break duration.
    /// <seealso href="http://github.com/App-vNext/Polly/wiki/Advanced-Circuit-Breaker" />
    /// </remarks>
    IAsyncPolicy<TResult> CreateCircuitBreakerPolicy<TResult>(string policyName, CircuitBreakerPolicyOptions<TResult> options);

    /// <summary>
    /// Creates a retry policy.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="options">The configuration of the retry policy, <see cref="RetryPolicyOptions"/>.</param>
    /// <returns>A retry policy.</returns>
    IAsyncPolicy CreateRetryPolicy(string policyName, RetryPolicyOptions options);

    /// <summary>
    /// Creates a retry policy.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="options">The configuration of the retry policy, <see cref="RetryPolicyOptions{TResult}"/>.</param>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policy.</typeparam>
    /// <returns>A retry policy.</returns>
    IAsyncPolicy<TResult> CreateRetryPolicy<TResult>(string policyName, RetryPolicyOptions<TResult> options);

    /// <summary>
    /// Creates a fallback policy to provide a substitute action in the event of failure.
    /// <see href="https://github.com/App-vNext/Polly/wiki/Fallback" />.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="provider">The task performed in the fallback scenario when the initial execution encounters a transient failure.</param>
    /// <param name="options">The options of the fallback policy.</param>
    /// <returns>
    /// A fallback policy.
    /// </returns>
    public IAsyncPolicy CreateFallbackPolicy(
        string policyName,
        FallbackScenarioTaskProvider provider,
        FallbackPolicyOptions options);

    /// <summary>
    /// Creates a fallback policy to provide a substitute action in the event of failure.
    /// <see href="https://github.com/App-vNext/Polly/wiki/Fallback" />.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="provider">The task performed in the fallback scenario when the initial execution encounters a transient failure.</param>
    /// <param name="options">The options of the fallback policy.</param>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policy.</typeparam>
    /// <returns>
    /// A fallback policy.
    /// </returns>
    public IAsyncPolicy<TResult> CreateFallbackPolicy<TResult>(
        string policyName,
        FallbackScenarioTaskProvider<TResult> provider,
        FallbackPolicyOptions<TResult> options);

    /// <summary>
    /// Creates the hedging policy.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="provider">The hedged task provider.</param>
    /// <param name="options">The options.</param>
    /// <returns>
    /// A hedging policy.
    /// </returns>
    public IAsyncPolicy CreateHedgingPolicy(
        string policyName,
        HedgedTaskProvider provider,
        HedgingPolicyOptions options);

    /// <summary>
    /// Creates the hedging policy.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="provider">The hedged task provider.</param>
    /// <param name="options">The options.</param>
    /// <typeparam name="TResult">The type of the result returned by the action executed by the policy.</typeparam>
    /// <returns>
    /// A hedging policy.
    /// </returns>
    public IAsyncPolicy<TResult> CreateHedgingPolicy<TResult>(
        string policyName,
        HedgedTaskProvider<TResult> provider,
        HedgingPolicyOptions<TResult> options);

    /// <summary>
    /// Creates a timeout policy to ensure the caller never has to wait beyond the configured timeout.
    /// <see href="https://github.com/App-vNext/Polly/wiki/Timeout" />.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="options">The options.</param>
    /// <returns>
    /// Timeout policy.
    /// </returns>
    public IAsyncPolicy CreateTimeoutPolicy(string policyName, TimeoutPolicyOptions options);

    /// <summary>
    /// Creates a bulkhead policy to limit the resources consumable by the governed actions,
    /// such that a fault 'storm' cannot cause a cascading failure also bringing down other operations.
    /// <see href="https://github.com/App-vNext/Polly/wiki/Bulkhead" />.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="options">The options.</param>
    /// <returns>
    /// A bulkhead policy.
    /// </returns>
    public IAsyncPolicy CreateBulkheadPolicy(string policyName, BulkheadPolicyOptions options);
}
