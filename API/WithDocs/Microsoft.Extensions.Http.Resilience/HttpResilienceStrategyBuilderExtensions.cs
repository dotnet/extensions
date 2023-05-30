// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Extensions for <see cref="T:Microsoft.Extensions.Http.Resilience.IHttpResilienceStrategyBuilder" />.
/// </summary>
public static class HttpResilienceStrategyBuilderExtensions
{
    /// <summary>
    /// Instructs the underlying builder to select the strategy instance by redacted authority (scheme + host + port).
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="classification">The data class associated with the authority.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>The authority is redacted using <see cref="T:Microsoft.Extensions.Compliance.Redaction.Redactor" /> retrieved for <paramref name="classification" />.</remarks>
    public static IHttpResilienceStrategyBuilder SelectStrategyByAuthority(this IHttpResilienceStrategyBuilder builder, DataClassification classification);

    /// <summary>
    /// Instructs the underlying builder to select the strategy instance by custom selector.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="selectorFactory">The factory that returns a key selector.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>The strategy key is used in metrics and logs, do not return any sensitive value.</remarks>
    public static IHttpResilienceStrategyBuilder SelectStrategyBy(this IHttpResilienceStrategyBuilder builder, Func<IServiceProvider, Func<HttpRequestMessage, string>> selectorFactory);
}
