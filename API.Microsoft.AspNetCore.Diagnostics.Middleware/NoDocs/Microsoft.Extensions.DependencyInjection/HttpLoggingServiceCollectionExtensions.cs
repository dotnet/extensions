// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Diagnostics.Logging;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

[Experimental("EXTEXP0013", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class HttpLoggingServiceCollectionExtensions
{
    public static IServiceCollection AddHttpLoggingRedaction(this IServiceCollection services, Action<LoggingRedactionOptions>? configure = null);
    public static IServiceCollection AddHttpLoggingRedaction(this IServiceCollection services, IConfigurationSection section);
    public static IServiceCollection AddHttpLogEnricher<T>(this IServiceCollection services) where T : class, IHttpLogEnricher;
}
