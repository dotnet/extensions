// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Diagnostics.Logging;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to register the HTTP logging feature within the service.
/// </summary>
[Experimental("EXTEXP0013", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class HttpLoggingServiceCollectionExtensions
{
    /// <summary>
    /// Enables enrichment and redaction of HTTP request logging output.
    /// </summary>
    /// <remarks>
    /// This will enable <see cref="P:Microsoft.AspNetCore.HttpLogging.HttpLoggingOptions.CombineLogs" /> and <see cref="F:Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.Duration" /> by default.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the redaction options.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddHttpLoggingRedaction(this IServiceCollection services, Action<LoggingRedactionOptions>? configure = null);

    /// <summary>
    /// Enables enrichment and redaction of HTTP request logging output.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="section">The configuration section with the redaction settings.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddHttpLoggingRedaction(this IServiceCollection services, IConfigurationSection section);

    /// <summary>
    /// Adds an enricher instance of <typeparamref name="T" /> to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to enrich incoming HTTP requests logs.
    /// </summary>
    /// <typeparam name="T">Type of enricher.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the instance of <typeparamref name="T" /> to.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddHttpLogEnricher<T>(this IServiceCollection services) where T : class, IHttpLogEnricher;
}
