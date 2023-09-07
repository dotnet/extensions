// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Telemetry.Logging;

public static class HttpClientLoggingExtensions
{
    public static IServiceCollection AddDefaultHttpClientLogging(this IServiceCollection services);
    public static IServiceCollection AddDefaultHttpClientLogging(this IServiceCollection services, IConfigurationSection section);
    public static IServiceCollection AddDefaultHttpClientLogging(this IServiceCollection services, Action<LoggingOptions> configure);
    public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder);
    public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder, IConfigurationSection section);
    public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder, Action<LoggingOptions> configure);
    public static IServiceCollection AddHttpClientLogEnricher<T>(this IServiceCollection services) where T : class, IHttpClientLogEnricher;
}
