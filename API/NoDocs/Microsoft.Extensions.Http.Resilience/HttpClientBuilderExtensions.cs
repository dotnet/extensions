// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Microsoft.Extensions.Http.Resilience;

public static class HttpClientBuilderExtensions
{
    public static IStandardHedgingHandlerBuilder AddStandardHedgingHandler(this IHttpClientBuilder builder, Action<IRoutingStrategyBuilder> configure);
    public static IStandardHedgingHandlerBuilder AddStandardHedgingHandler(this IHttpClientBuilder builder);
    public static IHttpResilienceStrategyBuilder AddResilienceHandler(this IHttpClientBuilder builder, string strategyName, Action<ResilienceStrategyBuilder<HttpResponseMessage>> configure);
    public static IHttpResilienceStrategyBuilder AddResilienceHandler(this IHttpClientBuilder builder, string strategyName, Action<ResilienceStrategyBuilder<HttpResponseMessage>, ResilienceHandlerContext> configure);
    public static IHttpStandardResilienceStrategyBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder, IConfigurationSection section);
    public static IHttpStandardResilienceStrategyBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder, Action<HttpStandardResilienceOptions> configure);
    public static IHttpStandardResilienceStrategyBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder);
}
