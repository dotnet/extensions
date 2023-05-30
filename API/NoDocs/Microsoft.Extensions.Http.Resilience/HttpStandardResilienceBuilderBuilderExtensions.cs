// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Http.Resilience;

public static class HttpStandardResilienceBuilderBuilderExtensions
{
    public static IHttpStandardResilienceStrategyBuilder Configure(this IHttpStandardResilienceStrategyBuilder builder, IConfigurationSection section);
    public static IHttpStandardResilienceStrategyBuilder Configure(this IHttpStandardResilienceStrategyBuilder builder, Action<HttpStandardResilienceOptions> configure);
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IHttpStandardResilienceStrategyBuilder Configure(this IHttpStandardResilienceStrategyBuilder builder, Action<HttpStandardResilienceOptions, IServiceProvider> configure);
    public static IHttpStandardResilienceStrategyBuilder SelectStrategyByAuthority(this IHttpStandardResilienceStrategyBuilder builder, DataClassification classification);
    public static IHttpStandardResilienceStrategyBuilder SelectStrategyBy(this IHttpStandardResilienceStrategyBuilder builder, Func<IServiceProvider, Func<HttpRequestMessage, string>> selectorFactory);
}
