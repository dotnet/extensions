// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Diagnostics.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class HttpLoggingServiceCollectionExtensions
{
    public static IServiceCollection AddHttpLogging(this IServiceCollection services);
    public static IServiceCollection AddHttpLogging(this IServiceCollection services, Action<LoggingOptions> configure);
    public static IServiceCollection AddHttpLogging(this IServiceCollection services, IConfigurationSection section);
    public static IServiceCollection AddHttpLogEnricher<T>(this IServiceCollection services) where T : class, IHttpLogEnricher;
}
