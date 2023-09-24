// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Extensions for <see cref="T:Microsoft.Extensions.Http.Resilience.IHttpStandardResiliencePipelineBuilder" />.
/// </summary>
public static class HttpStandardResiliencePipelineBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions" /> for the standard resilience pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="section">The section that the options will bind against.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    public static IHttpStandardResiliencePipelineBuilder Configure(this IHttpStandardResiliencePipelineBuilder builder, IConfigurationSection section);

    /// <summary>
    /// Configures the <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions" /> for the standard resilience pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="configure">The configure method.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    public static IHttpStandardResiliencePipelineBuilder Configure(this IHttpStandardResiliencePipelineBuilder builder, Action<HttpStandardResilienceOptions> configure);

    /// <summary>
    /// Configures the <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions" /> for the standard resilience pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="configure">The configure method.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IHttpStandardResiliencePipelineBuilder Configure(this IHttpStandardResiliencePipelineBuilder builder, Action<HttpStandardResilienceOptions, IServiceProvider> configure);

    /// <summary>
    /// Instructs the underlying builder to select the pipeline instance by redacted authority (scheme + host + port).
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="classification">The data class associated with the authority.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <remarks>The authority is redacted using <see cref="T:Microsoft.Extensions.Compliance.Redaction.Redactor" /> retrieved for <paramref name="classification" />.</remarks>
    public static IHttpStandardResiliencePipelineBuilder SelectPipelineByAuthority(this IHttpStandardResiliencePipelineBuilder builder, DataClassification classification);

    /// <summary>
    /// Instructs the underlying builder to select the pipeline instance by custom selector.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="selectorFactory">The factory that returns a key selector.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <remarks>The pipeline key is used in metrics and logs, do not return any sensitive value.</remarks>
    public static IHttpStandardResiliencePipelineBuilder SelectPipelineBy(this IHttpStandardResiliencePipelineBuilder builder, Func<IServiceProvider, Func<HttpRequestMessage, string>> selectorFactory);
}
