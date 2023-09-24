// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.
/// </summary>
public static class HttpResilienceHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds a resilience pipeline handler that uses a named inline resilience pipeline.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="pipelineName">The custom identifier for the resilience pipeline, used in the name of the pipeline.</param>
    /// <param name="configure">The callback that configures the pipeline.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
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
    /// <returns>The value of <paramref name="builder" />.</returns>
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
    /// <returns>The value of <paramref name="builder" />.</returns>
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
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <remarks>
    /// The resilience pipeline combines multiple strategies that are configured based on HTTP-specific <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions" /> options with recommended defaults.
    /// See <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions" /> for more details about the individual resilience strategies configured by this method.
    /// </remarks>
    public static IHttpStandardResiliencePipelineBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder, Action<HttpStandardResilienceOptions> configure);

    /// <summary>
    /// Adds a standard resilience handler that uses multiple resilience strategies with default options to send the requests and handle any transient errors.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <remarks>
    /// The resilience pipeline combines multiple strategies that are configured based on HTTP-specific <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions" /> options with recommended defaults.
    /// See <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions" /> for more details about the individual resilience strategies configured by this method.
    /// </remarks>
    public static IHttpStandardResiliencePipelineBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder);
}
