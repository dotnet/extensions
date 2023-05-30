// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Http.Resilience;

public static class StandardHedgingHandlerBuilderExtensions
{
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, IConfigurationSection section);
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, Action<HttpStandardHedgingResilienceOptions> configure);
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IStandardHedgingHandlerBuilder Configure(this IStandardHedgingHandlerBuilder builder, Action<HttpStandardHedgingResilienceOptions, IServiceProvider> configure);
    public static IStandardHedgingHandlerBuilder SelectStrategyByAuthority(this IStandardHedgingHandlerBuilder builder, DataClassification classification);
    public static IStandardHedgingHandlerBuilder SelectStrategyBy(this IStandardHedgingHandlerBuilder builder, Func<IServiceProvider, Func<HttpRequestMessage, string>> selectorFactory);
}
