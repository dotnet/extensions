// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Options for resilience strategies for usage in HTTP scenarios.
/// </summary>
/// <remarks>
/// These options represent configuration for five chained resilience strategies in this order (from the outermost to the innermost):
/// <para>
/// Bulkhead -&gt; Total Request Timeout -&gt; Retry -&gt; Circuit Breaker -&gt; Attempt Timeout.
/// </para>
/// The configuration of each pipeline is initialized with the default options per type. The request goes through these strategies:
/// <list type="number">
/// <item><description>Total request timeout pipeline applies an overall timeout to the execution,
/// ensuring that the request including hedging attempts, does not exceed the configured limit.</description></item>
/// <item><description>The retry pipeline retries the request in case the dependency is slow or returns a transient error.</description></item>
/// <item><description>The bulkhead pipeline limits the maximum number of concurrent requests being send to the dependency.</description></item>
/// <item><description>The circuit breaker blocks the execution if too many direct failures or timeouts are detected.</description></item>
/// <item><description>The attempt timeout pipeline limits each request attempt duration and throws if its exceeded.</description></item>
/// </list>
/// </remarks>
public class HttpStandardResilienceOptions
{
    /// <summary>
    /// Gets or sets the bulkhead options.
    /// </summary>
    /// <remarks>
    /// By default, this property is initialized with a unique instance of <see cref="T:Microsoft.Extensions.Http.Resilience.HttpRateLimiterStrategyOptions" /> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpRateLimiterStrategyOptions RateLimiterOptions { get; set; }

    /// <summary>
    /// Gets or sets the timeout Strategy options for the total timeout applied on the request's execution.
    /// </summary>
    /// <remarks>
    /// By default, this property is initialized with a unique instance of <see cref="T:Microsoft.Extensions.Http.Resilience.HttpTimeoutStrategyOptions" />.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpTimeoutStrategyOptions TotalRequestTimeoutOptions { get; set; }

    /// <summary>
    /// Gets or sets the retry pipeline options.
    /// </summary>
    /// <remarks>
    /// By default, this property is initialized with a unique instance of <see cref="T:Microsoft.Extensions.Http.Resilience.HttpRetryStrategyOptions" /> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpRetryStrategyOptions RetryOptions { get; set; }

    /// <summary>
    /// Gets or sets the circuit breaker options.
    /// </summary>
    /// <remarks>
    /// By default, this property is initialized with a unique instance of <see cref="T:Microsoft.Extensions.Http.Resilience.HttpCircuitBreakerStrategyOptions" /> using default properties values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpCircuitBreakerStrategyOptions CircuitBreakerOptions { get; set; }

    /// <summary>
    /// Gets or sets the options for the timeout pipeline applied per each request attempt.
    /// </summary>
    /// <remarks>
    /// By default, this property is initialized with a unique instance of <see cref="T:Microsoft.Extensions.Http.Resilience.HttpTimeoutStrategyOptions" />
    /// using custom <see cref="P:Polly.Timeout.TimeoutStrategyOptions.Timeout" /> of 10 seconds.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpTimeoutStrategyOptions AttemptTimeoutOptions { get; set; }

    public HttpStandardResilienceOptions();
}
