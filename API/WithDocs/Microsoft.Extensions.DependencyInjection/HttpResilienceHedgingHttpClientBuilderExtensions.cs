// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.
/// </summary>
public static class HttpResilienceHedgingHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds a standard hedging handler that wraps the execution of the request with a standard hedging mechanism.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="configure">Configures the routing strategy associated with this handler.</param>
    /// <returns>
    /// A <see cref="T:Microsoft.Extensions.Http.Resilience.IStandardHedgingHandlerBuilder" /> instance that can be used to configure the standard hedging behavior.
    /// </returns>
    /// <remarks>
    /// The standard hedging uses a pool of circuit breakers to ensure that unhealthy endpoints are not hedged against.
    /// By default, the selection from pool is based on the URL Authority (scheme + host + port).
    /// It is recommended that you configure the way the strategies are selected by calling
    /// <see cref="M:Microsoft.Extensions.Http.Resilience.StandardHedgingHandlerBuilderExtensions.SelectPipelineByAuthority(Microsoft.Extensions.Http.Resilience.IStandardHedgingHandlerBuilder)" />
    /// extensions.
    /// <para>
    /// See <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardHedgingResilienceOptions" /> for more details about the used resilience strategies.
    /// </para>
    /// </remarks>
    public static IStandardHedgingHandlerBuilder AddStandardHedgingHandler(this IHttpClientBuilder builder, Action<IRoutingStrategyBuilder> configure);

    /// <summary>
    /// Adds a standard hedging handler that wraps the execution of the request with a standard hedging mechanism.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <returns>
    /// A <see cref="T:Microsoft.Extensions.Http.Resilience.IStandardHedgingHandlerBuilder" /> instance that can be used to configure the standard hedging behavior.
    /// </returns>
    /// <remarks>
    /// The standard hedging uses a pool of circuit breakers to ensure that unhealthy endpoints are not hedged against.
    /// By default, the selection from pool is based on the URL Authority (scheme + host + port).
    /// It is recommended that you configure the way the strategies are selected by calling
    /// <see cref="M:Microsoft.Extensions.Http.Resilience.StandardHedgingHandlerBuilderExtensions.SelectPipelineByAuthority(Microsoft.Extensions.Http.Resilience.IStandardHedgingHandlerBuilder)" />
    /// extensions.
    /// <para>
    /// See <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardHedgingResilienceOptions" /> for more details about the used resilience strategies.
    /// </para>
    /// </remarks>
    public static IStandardHedgingHandlerBuilder AddStandardHedgingHandler(this IHttpClientBuilder builder);
}
