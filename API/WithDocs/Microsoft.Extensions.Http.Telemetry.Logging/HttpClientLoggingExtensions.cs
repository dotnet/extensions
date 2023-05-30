// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Telemetry.Logging;

/// <summary>
/// Extension methods to register HTTP client logging feature.
/// </summary>
public static class HttpClientLoggingExtensions
{
    /// <summary>
    /// Adds a <see cref="T:System.Net.Http.DelegatingHandler" /> to collect and emit logs for outgoing requests for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request logs auto collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</param>
    /// <returns>
    /// <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> instance for chaining.
    /// </returns>
    public static IServiceCollection AddDefaultHttpClientLogging(this IServiceCollection services);

    /// <summary>
    /// Adds a <see cref="T:System.Net.Http.DelegatingHandler" /> to collect and emit logs for outgoing requests for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request logs auto collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</param>
    /// <param name="section">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> to use for configuring <see cref="T:Microsoft.Extensions.Http.Telemetry.Logging.LoggingOptions" />.</param>
    /// <returns>
    /// <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> instance for chaining.
    /// </returns>
    public static IServiceCollection AddDefaultHttpClientLogging(this IServiceCollection services, IConfigurationSection section);

    /// <summary>
    /// Adds a <see cref="T:System.Net.Http.DelegatingHandler" /> to collect and emit logs for outgoing requests for all http clients.
    /// </summary>
    /// <remarks>
    /// This extension configures outgoing request logs auto collection globally for all http clients.
    /// </remarks>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.</param>
    /// <param name="configure">The delegate to configure <see cref="T:Microsoft.Extensions.Http.Telemetry.Logging.LoggingOptions" /> with.</param>
    /// <returns>
    /// <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> instance for chaining.
    /// </returns>
    public static IServiceCollection AddDefaultHttpClientLogging(this IServiceCollection services, Action<LoggingOptions> configure);

    /// <summary>
    /// Registers HTTP client logging components into <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.</param>
    /// <returns>
    /// An <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    /// <exception cref="T:System.ArgumentNullException">Argument <paramref name="builder" /> is <see langword="null" />.</exception>
    public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder);

    /// <summary>
    /// Registers HTTP client logging components into <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.</param>
    /// <param name="section">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> to use for configuring <see cref="T:Microsoft.Extensions.Http.Telemetry.Logging.LoggingOptions" />.</param>
    /// <returns>
    /// An <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder, IConfigurationSection section);

    /// <summary>
    /// Registers HTTP client logging components into <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.</param>
    /// <param name="configure">The delegate to configure <see cref="T:Microsoft.Extensions.Http.Telemetry.Logging.LoggingOptions" /> with.</param>
    /// <returns>
    /// An <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static IHttpClientBuilder AddHttpClientLogging(this IHttpClientBuilder builder, Action<LoggingOptions> configure);

    /// <summary>
    /// Adds an enricher instance of <typeparamref name="T" /> to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to enrich HTTP client logs.
    /// </summary>
    /// <typeparam name="T">Type of enricher.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the instance of <typeparamref name="T" /> to.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    public static IServiceCollection AddHttpClientLogEnricher<T>(this IServiceCollection services) where T : class, IHttpClientLogEnricher;
}
