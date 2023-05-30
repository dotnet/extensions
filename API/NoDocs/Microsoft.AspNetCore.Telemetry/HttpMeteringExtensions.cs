// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Telemetry;

public static class HttpMeteringExtensions
{
    public static HttpMeteringBuilder AddMetricEnricher<T>(this HttpMeteringBuilder builder) where T : class, IIncomingRequestMetricEnricher;
    public static HttpMeteringBuilder AddMetricEnricher(this HttpMeteringBuilder builder, IIncomingRequestMetricEnricher enricher);
    public static IApplicationBuilder UseHttpMetering(this IApplicationBuilder builder);
    public static IServiceCollection AddHttpMetering(this IServiceCollection services);
    public static IServiceCollection AddHttpMetering(this IServiceCollection services, Action<HttpMeteringBuilder>? build);
}
