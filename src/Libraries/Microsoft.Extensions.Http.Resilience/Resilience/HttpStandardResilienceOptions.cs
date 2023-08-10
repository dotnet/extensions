// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Options;
using Polly.Timeout;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Options for resilience strategies for usage in HTTP scenarios.
/// </summary>
/// <remarks>
/// These options represent configuration for five chained resilience strategies in this order (from the outermost to the innermost):
/// <para>
/// Bulkhead -> Total Request Timeout -> Retry -> Circuit Breaker -> Attempt Timeout.
/// </para>
/// The configuration of each Strategy is initialized with the default options per type. The request goes through these strategies:
/// <list type="number">
/// <item><description>Total request timeout Strategy applies an overall timeout to the execution, ensuring that the request including hedging attempts does not exceed the configured limit.</description></item>
/// <item><description>The retry Strategy retries the request in case the dependency is slow or returns a transient error.</description></item>
/// <item><description>The bulkhead Strategy limits the maximum number of concurrent requests being send to the dependency.</description></item>
/// <item><description>The circuit breaker blocks the execution if too many direct failures or timeouts are detected.</description></item>
/// <item><description>The attempt timeout Strategy limits each request attempt duration and throws if its exceeded.</description></item>
/// </list>
/// </remarks>
public class HttpStandardResilienceOptions
{
    /// <summary>
    /// Gets or sets the bulkhead options.
    /// </summary>
    /// <remarks>
    /// By default, this property is initialized with a unique instance of <see cref="HttpRateLimiterStrategyOptions"/> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpRateLimiterStrategyOptions RateLimiterOptions { get; set; } = new HttpRateLimiterStrategyOptions
    {
        Name = StandardStrategyNames.RateLimiter
    };

    /// <summary>
    /// Gets or sets the timeout Strategy options for the total timeout applied on the request's execution.
    /// </summary>
    /// <remarks>
    /// By default, this property is initialized with a unique instance of <see cref="HttpTimeoutStrategyOptions"/>.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpTimeoutStrategyOptions TotalRequestTimeoutOptions { get; set; } = new HttpTimeoutStrategyOptions
    {
        Name = StandardStrategyNames.TotalRequestTimeout
    };

    /// <summary>
    /// Gets or sets the retry strategy options.
    /// </summary>
    /// <remarks>
    /// By default, this property is initialized with a unique instance of <see cref="HttpRetryStrategyOptions"/> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpRetryStrategyOptions RetryOptions { get; set; } = new HttpRetryStrategyOptions
    {
        Name = StandardStrategyNames.Retry
    };

    /// <summary>
    /// Gets or sets the circuit breaker options.
    /// </summary>
    /// <remarks>
    /// By default, this property is initialized with a unique instance of <see cref="HttpCircuitBreakerStrategyOptions"/> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpCircuitBreakerStrategyOptions CircuitBreakerOptions { get; set; } = new HttpCircuitBreakerStrategyOptions
    {
        Name = StandardStrategyNames.CircuitBreaker
    };

    /// <summary>
    /// Gets or sets the options for the timeout strategy applied per each request attempt.
    /// </summary>
    /// <remarks>
    /// By default, this property is initialized with a unique instance of <see cref="HttpTimeoutStrategyOptions"/>
    /// using custom <see cref="TimeoutStrategyOptions.Timeout"/> of 10 seconds.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpTimeoutStrategyOptions AttemptTimeoutOptions { get; set; } = new()
    {
        Timeout = TimeSpan.FromSeconds(10),
        Name = StandardStrategyNames.AttemptTimeout
    };
}
