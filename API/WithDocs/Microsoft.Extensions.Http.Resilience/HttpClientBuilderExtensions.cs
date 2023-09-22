// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Extensions for <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.
/// </summary>
/// <summary>
/// Extensions for <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.
/// </summary>
public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Adds a standard hedging handler that wraps the execution of the request with a standard hedging mechanism.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="configure">Configures the routing strategy associated with this handler.</param>
    /// <returns>
    /// A <see cref="T:Microsoft.Extensions.Http.Resilience.IStandardHedgingHandlerBuilder" /> builder that can be used to configure the standard hedging behavior.
    /// </returns>
    /// <remarks>
    /// The standard hedging uses a pool of circuit breakers to ensure that unhealthy endpoints are not hedged against.
    /// By default, the selection from pool is based on the URL Authority (scheme + host + port).
    /// It is recommended that you configure the way the strategies are selected by calling
    /// <see cref="M:Microsoft.Extensions.Http.Resilience.StandardHedgingHandlerBuilderExtensions.SelectPipelineByAuthority(Microsoft.Extensions.Http.Resilience.IStandardHedgingHandlerBuilder,Microsoft.Extensions.Compliance.Classification.DataClassification)" />
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
    /// A <see cref="T:Microsoft.Extensions.Http.Resilience.IStandardHedgingHandlerBuilder" /> builder that can be used to configure the standard hedging behavior.
    /// </returns>
    /// <remarks>
    /// The standard hedging uses a pool of circuit breakers to ensure that unhealthy endpoints are not hedged against.
    /// By default, the selection from pool is based on the URL Authority (scheme + host + port).
    /// It is recommended that you configure the way the strategies are selected by calling
    /// <see cref="M:Microsoft.Extensions.Http.Resilience.StandardHedgingHandlerBuilderExtensions.SelectPipelineByAuthority(Microsoft.Extensions.Http.Resilience.IStandardHedgingHandlerBuilder,Microsoft.Extensions.Compliance.Classification.DataClassification)" />
    /// extensions.
    /// <para>
    /// See <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardHedgingResilienceOptions" /> for more details about the used resilience strategies.
    /// </para>
    /// </remarks>
    public static IStandardHedgingHandlerBuilder AddStandardHedgingHandler(this IHttpClientBuilder builder);

    /// <summary>
    /// Adds a resilience pipeline handler that uses a named inline resilience pipeline.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="pipelineName">The custom identifier for the resilience pipeline, used in the name of the pipeline.</param>
    /// <param name="configure">The callback that configures the pipeline.</param>
    /// <returns>The HTTP pipeline builder instance.</returns>
    /// <remarks>
    /// The final pipeline name is combination of <see cref="P:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder.Name" /> and <paramref name="pipelineName" />.
    /// Use pipeline name identifier if your HTTP client contains multiple resilience handlers.
    /// </remarks>
    public static IHttpResiliencePipelineBuilder AddResilienceHandler(this IHttpClientBuilder builder, string pipelineName, Action<ResiliencePipelineBuilder<HttpResponseMessage>> configure);

    /// <summary>
    /// Adds a resilience pipeline handler that uses a named inline resilience pipeline.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="pipelineName">The custom identifier for the resilience pipeline, used in the name of the pipeline.</param>
    /// <param name="configure">The callback that configures the pipeline.</param>
    /// <returns>The HTTP pipeline builder instance.</returns>
    /// <remarks>
    /// The final pipeline name is combination of <see cref="P:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder.Name" /> and <paramref name="pipelineName" />.
    /// Use pipeline name identifier if your HTTP client contains multiple resilience handlers.
    /// </remarks>
    public static IHttpResiliencePipelineBuilder AddResilienceHandler(this IHttpClientBuilder builder, string pipelineName, Action<ResiliencePipelineBuilder<HttpResponseMessage>, ResilienceHandlerContext> configure);

    /// <summary>
    /// Adds a standard resilience handler that uses multiple resilience strategies with default options to send the requests and handle any transient errors.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="section">The section that the options will bind against.</param>
    /// <returns>The HTTP resilience handler builder instance.</returns>
    /// <remarks>
    /// The resilience pipeline combines multiple strategies that are configured based on HTTP-specific <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions" /> options with recommended defaults.
    /// See <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions" /> for more details about the individual resilience strategies configured by this method.
    /// </remarks>
    public static IHttpStandardResiliencePipelineBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder, IConfigurationSection section);

    /// <summary>
    /// Adds a standard resilience handler that uses multiple resilience strategies with default options to send the requests and handle any transient errors.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="configure">The callback that configures the options.</param>
    /// <returns>The HTTP resilience handler builder instance.</returns>
    /// <remarks>
    /// The resilience pipeline combines multiple strategies that are configured based on HTTP-specific <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions" /> options with recommended defaults.
    /// See <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions" /> for more details about the individual resilience strategies configured by this method.
    /// </remarks>
    public static IHttpStandardResiliencePipelineBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder, Action<HttpStandardResilienceOptions> configure);

    /// <summary>
    /// Adds a standard resilience handler that uses multiple resilience strategies with default options to send the requests and handle any transient errors.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <returns>The HTTP resilience handler builder instance.</returns>
    /// <remarks>
    /// The resilience pipeline combines multiple strategies that are configured based on HTTP-specific <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions" /> options with recommended defaults.
    /// See <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions" /> for more details about the individual resilience strategies configured by this method.
    /// </remarks>
    public static IHttpStandardResiliencePipelineBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder);
}
