// Assembly 'Microsoft.Extensions.Http.Diagnostics'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Logging;

namespace Microsoft.Extensions.DependencyInjection;

public static class HttpClientLoggingServiceCollectionExtensions
{
    public static IServiceCollection AddExtendedHttpClientLogging(this IServiceCollection services);
    public static IServiceCollection AddExtendedHttpClientLogging(this IServiceCollection services, IConfigurationSection section);
    public static IServiceCollection AddExtendedHttpClientLogging(this IServiceCollection services, Action<LoggingOptions> configure);
    public static IServiceCollection AddHttpClientLogEnricher<T>(this IServiceCollection services) where T : class, IHttpClientLogEnricher;
}
