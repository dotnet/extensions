// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Options for the pipeline of resilience strategies for usage in hedging HTTP scenarios.
/// </summary>
/// /// <remarks>
/// These options represents configuration for 5 chained layers in this order (from the outermost to the innermost):
/// <para>
/// Total Request Timeout -&gt; Hedging -&gt; Bulkhead (per endpoint) -&gt; Circuit Breaker (per endpoint) -&gt; Attempt Timeout (per endpoint).
/// </para>
/// The configuration of each resilience strategy is initialized with the default options per type. The request goes through these resilience strategies:
/// <para>
/// <list type="number">
/// <item><description>Total request timeout strategy applies an overall timeout to the execution,
/// ensuring that the request including hedging attempts does not exceed the configured limit.</description></item>
/// <item><description>The hedging strategy executes the requests against multiple endpoints in case the dependency is slow or returns a transient error.</description></item>
/// <item><description>The bulkhead policy limits the maximum number of concurrent requests being send to the dependency.</description></item>
/// <item><description>The circuit breaker blocks the execution if too many direct failures or timeouts are detected.</description></item>
/// <item><description>The attempt timeout strategy limits each request attempt duration and throws if its exceeded.</description></item>
/// </list>
/// </para>
/// The last three strategies are assigned to each individual endpoint. The selection of endpoint can be customized by
/// <see cref="M:Microsoft.Extensions.Http.Resilience.StandardHedgingHandlerBuilderExtensions.SelectPipelineByAuthority(Microsoft.Extensions.Http.Resilience.IStandardHedgingHandlerBuilder)" /> or
/// <see cref="M:Microsoft.Extensions.Http.Resilience.StandardHedgingHandlerBuilderExtensions.SelectPipelineBy(Microsoft.Extensions.Http.Resilience.IStandardHedgingHandlerBuilder,System.Func{System.IServiceProvider,System.Func{System.Net.Http.HttpRequestMessage,System.String}})" /> extensions.
/// <para>
/// By default, the endpoint is selected by authority (scheme + host + port).
/// </para>
/// </remarks>
public class HttpStandardHedgingResilienceOptions
{
    /// <summary>
    /// Gets or sets the timeout strategy options for the total timeout applied on the request execution.
    /// </summary>
    /// <remarks>
    /// By default, this property is initialized with a unique instance of <see cref="T:Microsoft.Extensions.Http.Resilience.HttpTimeoutStrategyOptions" />
    /// using default property values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpTimeoutStrategyOptions TotalRequestTimeoutOptions { get; set; }

    /// <summary>
    /// Gets or sets the hedging strategy options.
    /// </summary>
    /// <remarks>
    /// By default, this property is initialized with a unique instance of <see cref="T:Microsoft.Extensions.Http.Resilience.HttpHedgingStrategyOptions" /> using default property values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HttpHedgingStrategyOptions HedgingOptions { get; set; }

    /// <summary>
    /// Gets or sets the hedging endpoint options.
    /// </summary>
    /// <remarks>
    /// By default, this property is initialized with a unique instance of <see cref="T:Microsoft.Extensions.Http.Resilience.HedgingEndpointOptions" /> using default property values.
    /// </remarks>
    [Required]
    [ValidateObjectMembers]
    public HedgingEndpointOptions EndpointOptions { get; set; }

    public HttpStandardHedgingResilienceOptions();
}
