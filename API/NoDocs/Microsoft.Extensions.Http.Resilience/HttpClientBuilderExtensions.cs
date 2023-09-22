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
    public static IHttpResiliencePipelineBuilder AddResilienceHandler(this IHttpClientBuilder builder, string pipelineName, Action<ResiliencePipelineBuilder<HttpResponseMessage>> configure);
    public static IHttpResiliencePipelineBuilder AddResilienceHandler(this IHttpClientBuilder builder, string pipelineName, Action<ResiliencePipelineBuilder<HttpResponseMessage>, ResilienceHandlerContext> configure);
    public static IHttpStandardResiliencePipelineBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder, IConfigurationSection section);
    public static IHttpStandardResiliencePipelineBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder, Action<HttpStandardResilienceOptions> configure);
    public static IHttpStandardResiliencePipelineBuilder AddStandardResilienceHandler(this IHttpClientBuilder builder);
}
