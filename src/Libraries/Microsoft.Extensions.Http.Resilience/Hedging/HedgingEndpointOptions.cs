// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Http.Resilience.Internal.Validators;
using Microsoft.Extensions.Options;
using Polly.Timeout;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Options for the pipeline of resilience strategies assigned to a particular endpoint.
/// </summary>
/// <remarks>
/// It is using three chained layers in this order (from the outermost to the innermost): Bulkhead -> Circuit Breaker -> Attempt Timeout.
/// </remarks>
public class HedgingEndpointOptions
{
    /// <summary>
    /// Gets or sets the bulkhead options for the endpoint.
    /// </summary>
    /// <remarks>
    /// By default it is initialized with a unique instance of <see cref="HttpRateLimiterStrategyOptions"/> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpRateLimiterStrategyOptions RateLimiter { get; set; } = new HttpRateLimiterStrategyOptions
    {
        Name = StandardHedgingPipelineNames.RateLimiter
    };

    /// <summary>
    /// Gets or sets the circuit breaker options for the endpoint.
    /// </summary>
    /// <remarks>
    /// By default it is initialized with a unique instance of <see cref="HttpCircuitBreakerStrategyOptions"/> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpCircuitBreakerStrategyOptions CircuitBreaker { get; set; } = new HttpCircuitBreakerStrategyOptions
    {
        Name = StandardHedgingPipelineNames.CircuitBreaker
    };

    /// <summary>
    /// Gets or sets the options for the timeout resilience strategy applied per each request attempt.
    /// </summary>
    /// <remarks>
    /// By default it is initialized with a unique instance of <see cref="HttpTimeoutStrategyOptions"/>
    /// using a custom <see cref="TimeoutStrategyOptions.Timeout"/> of 10 seconds.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpTimeoutStrategyOptions Timeout { get; set; } = new()
    {
        Timeout = TimeSpan.FromSeconds(10),
        Name = StandardHedgingPipelineNames.AttemptTimeout
    };
}
