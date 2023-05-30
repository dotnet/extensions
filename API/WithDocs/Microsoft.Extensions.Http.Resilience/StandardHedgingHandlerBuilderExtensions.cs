// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Extensions for <see cref="T:Microsoft.Extensions.Http.Resilience.IStandardHedgingHandlerBuilder" />.
/// </summary>
public static class StandardHedgingHandlerBuilderExtensions
{
    /// <summary>
    /// Configures the <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardHedgingResilienceOptions" /> for the standard hedging strategy.
    /// </summary>
    /// <param name="builder">The strategy builder.</param>
    /// <param name="section">The section that the options will bind against.</param>
    /// <returns>The same builder instance.</returns>
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, IConfigurationSection section);

    /// <summary>
    /// Configures the <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions" /> for the standard hedging strategy.
    /// </summary>
    /// <param name="builder">The strategy builder.</param>
    /// <param name="configure">The configure method.</param>
    /// <returns>The same builder instance.</returns>
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, Action<HttpStandardHedgingResilienceOptions> configure);

    /// <summary>
    /// Configures the <see cref="T:Microsoft.Extensions.Http.Resilience.HttpStandardResilienceOptions" /> for the standard hedging strategy.
    /// </summary>
    /// <param name="builder">The strategy builder.</param>
    /// <param name="configure">The configure method.</param>
    /// <returns>The same builder instance.</returns>
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, Action<HttpStandardHedgingResilienceOptions, IServiceProvider> configure);

    /// <summary>
    /// Instructs the underlying strategy builder to select the strategy instance by redacted authority (scheme + host + port).
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="classification">The data class associated with the authority.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>The authority is redacted using <see cref="T:Microsoft.Extensions.Compliance.Redaction.Redactor" /> retrieved for <paramref name="classification" />.</remarks>
    public static IStandardHedgingHandlerBuilder SelectStrategyByAuthority(this IStandardHedgingHandlerBuilder builder, DataClassification classification);

    /// <summary>
    /// Instructs the underlying strategy builder to select the strategy instance by custom selector.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="selectorFactory">The factory that returns key selector.</param>
    /// <returns>The same builder instance.</returns>
    /// <remarks>The strategy key is used in metrics and logs, do not return any sensitive value.</remarks>
    public static IStandardHedgingHandlerBuilder SelectStrategyBy(this IStandardHedgingHandlerBuilder builder, Func<IServiceProvider, Func<HttpRequestMessage, string>> selectorFactory);
}
