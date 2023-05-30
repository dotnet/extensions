// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Http.Telemetry.Tracing;

public static class HttpClientTracingExtensions
{
    public static TracerProviderBuilder AddHttpClientTracing(this TracerProviderBuilder builder);
    public static TracerProviderBuilder AddHttpClientTracing(this TracerProviderBuilder builder, Action<HttpClientTracingOptions> configure);
    public static TracerProviderBuilder AddHttpClientTracing(this TracerProviderBuilder builder, IConfigurationSection section);
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddHttpClientTraceEnricher<T>(this IServiceCollection services) where T : class, IHttpClientTraceEnricher;
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddHttpClientTraceEnricher(this IServiceCollection services, IHttpClientTraceEnricher enricher);
    public static TracerProviderBuilder AddHttpClientTraceEnricher<T>(this TracerProviderBuilder builder) where T : class, IHttpClientTraceEnricher;
    public static TracerProviderBuilder AddHttpClientTraceEnricher(this TracerProviderBuilder builder, IHttpClientTraceEnricher enricher);
}
