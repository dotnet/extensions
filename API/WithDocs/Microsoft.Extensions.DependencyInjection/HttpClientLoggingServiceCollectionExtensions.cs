// Assembly 'Microsoft.Extensions.Http.Diagnostics'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to register HTTP client logging features.
/// </summary>
public static class HttpClientLoggingServiceCollectionExtensions
{
    /// <summary>
    /// Adds an <see cref="T:Microsoft.Extensions.Http.Logging.IHttpClientAsyncLogger" /> to emit logs for outgoing requests for all HTTP clients created with <see cref="T:System.Net.Http.IHttpClientFactory" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <remarks>
    /// All other loggers are removed - including the default one, registered via <see cref="M:Microsoft.Extensions.DependencyInjection.HttpClientBuilderExtensions.AddDefaultLogger(Microsoft.Extensions.DependencyInjection.IHttpClientBuilder)" />.
    /// </remarks>
    /// <exception cref="T:System.ArgumentNullException">Argument <paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddDefaultHttpClientLogging(this IServiceCollection services);

    /// <summary>
    /// Adds an <see cref="T:Microsoft.Extensions.Http.Logging.IHttpClientAsyncLogger" /> to emit logs for outgoing requests for all HTTP clients created with <see cref="T:System.Net.Http.IHttpClientFactory" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</param>
    /// <param name="section">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> to use for configuring <see cref="T:Microsoft.Extensions.Http.Logging.LoggingOptions" />.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <remarks>
    /// All other loggers are removed - including the default one, registered via <see cref="M:Microsoft.Extensions.DependencyInjection.HttpClientBuilderExtensions.AddDefaultLogger(Microsoft.Extensions.DependencyInjection.IHttpClientBuilder)" />.
    /// </remarks>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static IServiceCollection AddDefaultHttpClientLogging(this IServiceCollection services, IConfigurationSection section);

    /// <summary>
    /// Adds an <see cref="T:Microsoft.Extensions.Http.Logging.IHttpClientAsyncLogger" /> to emit logs for outgoing requests for all HTTP clients created with <see cref="T:System.Net.Http.IHttpClientFactory" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</param>
    /// <param name="configure">The delegate to configure <see cref="T:Microsoft.Extensions.Http.Logging.LoggingOptions" /> with.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <remarks>
    /// All other loggers are removed - including the default one, registered via <see cref="M:Microsoft.Extensions.DependencyInjection.HttpClientBuilderExtensions.AddDefaultLogger(Microsoft.Extensions.DependencyInjection.IHttpClientBuilder)" />.
    /// </remarks>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static IServiceCollection AddDefaultHttpClientLogging(this IServiceCollection services, Action<LoggingOptions> configure);

    /// <summary>
    /// Adds an enricher instance of <typeparamref name="T" /> to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to enrich <see cref="T:System.Net.Http.HttpClient" /> logs.
    /// </summary>
    /// <typeparam name="T">Type of enricher.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the instance of <typeparamref name="T" /> to.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddHttpClientLogEnricher<T>(this IServiceCollection services) where T : class, IHttpClientLogEnricher;
}
