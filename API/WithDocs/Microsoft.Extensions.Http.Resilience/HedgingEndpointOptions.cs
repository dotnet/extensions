// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Options for the pipeline of resilience strategies assigned to a particular endpoint.
/// </summary>
/// <remarks>
/// It is using three chained layers in this order (from the outermost to the innermost): Bulkhead -&gt; Circuit Breaker -&gt; Attempt Timeout.
/// </remarks>
public class HedgingEndpointOptions
{
    /// <summary>
    /// Gets or sets the bulkhead options for the endpoint.
    /// </summary>
    /// <remarks>
    /// By default it is initialized with a unique instance of <see cref="T:Microsoft.Extensions.Http.Resilience.HttpRateLimiterStrategyOptions" /> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpRateLimiterStrategyOptions RateLimiterOptions { get; set; }

    /// <summary>
    /// Gets or sets the circuit breaker options for the endpoint.
    /// </summary>
    /// <remarks>
    /// By default it is initialized with a unique instance of <see cref="T:Microsoft.Extensions.Http.Resilience.HttpCircuitBreakerStrategyOptions" /> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpCircuitBreakerStrategyOptions CircuitBreakerOptions { get; set; }

    /// <summary>
    /// Gets or sets the options for the timeout resilience strategy applied per each request attempt.
    /// </summary>
    /// <remarks>
    /// By default it is initialized with a unique instance of <see cref="T:Microsoft.Extensions.Http.Resilience.HttpTimeoutStrategyOptions" />
    /// using a custom <see cref="P:Polly.Timeout.TimeoutStrategyOptions.Timeout" /> of 10 seconds.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpTimeoutStrategyOptions TimeoutOptions { get; set; }

    public HedgingEndpointOptions();
}
