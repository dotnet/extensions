// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Net.Http;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Extensions for <see cref="T:Microsoft.Extensions.Http.Resilience.IHttpResiliencePipelineBuilder" />.
/// </summary>
public static class HttpResiliencePipelineBuilderExtensions
{
    /// <summary>
    /// Instructs the underlying builder to select the pipeline instance by redacted authority (scheme + host + port).
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    public static IHttpResiliencePipelineBuilder SelectPipelineByAuthority(this IHttpResiliencePipelineBuilder builder);

    /// <summary>
    /// Instructs the underlying builder to select the pipeline instance by custom selector.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="selectorFactory">The factory that returns a key selector.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <remarks>The pipeline key is used in metrics and logs, so don't return any sensitive values.</remarks>
    public static IHttpResiliencePipelineBuilder SelectPipelineBy(this IHttpResiliencePipelineBuilder builder, Func<IServiceProvider, Func<HttpRequestMessage, string>> selectorFactory);
}
