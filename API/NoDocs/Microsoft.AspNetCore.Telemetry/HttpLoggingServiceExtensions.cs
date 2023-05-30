// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Telemetry;

public static class HttpLoggingServiceExtensions
{
    public static IServiceCollection AddHttpLogging(this IServiceCollection services);
    public static IServiceCollection AddHttpLogging(this IServiceCollection services, Action<LoggingOptions> configure);
    public static IServiceCollection AddHttpLogging(this IServiceCollection services, IConfigurationSection section);
    public static IServiceCollection AddHttpLogEnricher<T>(this IServiceCollection services) where T : class, IHttpLogEnricher;
    public static IApplicationBuilder UseHttpLoggingMiddleware(this IApplicationBuilder builder);
}
