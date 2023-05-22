// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Resilience.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Options for resilient pipeline of policies assigned to a particular endpoint. It is using three chained layers in this order (from the outermost to the innermost):
/// Bulkhead -> Circuit Breaker -> Attempt Timeout.
/// </summary>
public class HedgingEndpointOptions
{
    private static readonly TimeSpan _timeoutInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the bulkhead options for the endpoint.
    /// </summary>
    /// <remarks>
    /// By default it is initialized with a unique instance of <see cref="HttpBulkheadPolicyOptions"/> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpBulkheadPolicyOptions BulkheadOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the circuit breaker options for the endpoint.
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
    /// using a custom <see cref="TimeoutPolicyOptions.TimeoutInterval"/> of 10 seconds.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpTimeoutPolicyOptions TimeoutOptions { get; set; } = new()
    {
        TimeoutInterval = _timeoutInterval,
    };
}
