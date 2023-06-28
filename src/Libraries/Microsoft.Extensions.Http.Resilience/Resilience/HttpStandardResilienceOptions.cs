// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Options for resilient pipeline of policies for usage in HTTP scenarios. It is using five chained layers in this order (from the outermost to the innermost):
/// Bulkhead -> Total Request Timeout -> Retry -> Circuit Breaker -> Attempt Timeout.
/// </summary>
/// /// <remarks>
/// The configuration of each policy is initialized with the default options per type. The request goes through these policies:
/// 1. Total request timeout policy applies an overall timeout to the execution, ensuring that the request including hedging attempts does not exceed the configured limit.
/// 2. The retry policy retries the request in case the dependency is slow or returns a transient error.
/// 3. The bulkhead policy limits the maximum number of concurrent requests being send to the dependency.
/// 4. The circuit breaker blocks the execution if too many direct failures or timeouts are detected.
/// 5. The attempt timeout policy limits each request attempt duration and throws if its exceeded.
/// </remarks>
public class HttpStandardResilienceOptions
{
    private static readonly TimeSpan _attemptTimeoutInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the bulkhead options.
    /// </summary>
    /// <remarks>
    /// By default it is initialized with a unique instance of <see cref="HttpBulkheadPolicyOptions"/> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpBulkheadPolicyOptions BulkheadOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the timeout policy options for the total timeout applied on the request's execution.
    /// </summary>
    /// <remarks>
    /// By default it is initialized with a unique instance of <see cref="HttpTimeoutPolicyOptions"/>
    /// using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpTimeoutPolicyOptions TotalRequestTimeoutOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the retry policy Options.
    /// </summary>
    /// <remarks>
    /// By default it is initialized with a unique instance of <see cref="HttpRetryPolicyOptions"/> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpRetryPolicyOptions RetryOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the circuit breaker options.
    /// </summary>
    /// <remarks>
    /// By default it is initialized with a unique instance of <see cref="HttpCircuitBreakerPolicyOptions"/> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpCircuitBreakerPolicyOptions CircuitBreakerOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the options for the timeout policy applied per each request attempt.
    /// </summary>
    /// <remarks>
    /// By default it is initialized with a unique instance of <see cref="HttpTimeoutPolicyOptions"/>
    /// using custom <see cref="TimeoutPolicyOptions.TimeoutInterval"/> of 10 seconds.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpTimeoutPolicyOptions AttemptTimeoutOptions { get; set; } = new()
    {
        TimeoutInterval = _attemptTimeoutInterval,
    };
}
