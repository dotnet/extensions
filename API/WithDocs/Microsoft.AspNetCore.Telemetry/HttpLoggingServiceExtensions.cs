// Assembly 'Microsoft.AspNetCore.Telemetry.Middleware'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Extension methods to register the HTTP logging feature within the service.
/// </summary>
public static class HttpLoggingServiceExtensions
{
    /// <summary>
    /// Adds components for incoming HTTP requests logging into <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddHttpLogging(this IServiceCollection services);

    /// <summary>
    /// Adds components for incoming HTTP requests logging into <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <param name="configure">
    /// An <see cref="T:System.Action`1" /> to configure the <see cref="T:Microsoft.AspNetCore.Telemetry.LoggingOptions" />.
    /// </param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// Either <paramref name="services" /> or <paramref name="configure" /> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddHttpLogging(this IServiceCollection services, Action<LoggingOptions> configure);

    /// <summary>
    /// Adds components for incoming HTTP requests logging into <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
    /// <param name="section">The configuration section to bind <see cref="T:Microsoft.AspNetCore.Telemetry.LoggingOptions" /> to.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// Either <paramref name="services" /> or <paramref name="section" /> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddHttpLogging(this IServiceCollection services, IConfigurationSection section);

    /// <summary>
    /// Adds an enricher instance of <typeparamref name="T" /> to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to enrich incoming HTTP requests logs.
    /// </summary>
    /// <typeparam name="T">Type of enricher.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the instance of <typeparamref name="T" /> to.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddHttpLogEnricher<T>(this IServiceCollection services) where T : class, IHttpLogEnricher;

    /// <summary>
    /// Registers incoming HTTP request logging middleware into <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" />.
    /// </summary>
    /// <remarks>
    /// Request logging middleware should be placed after <see cref="M:Microsoft.AspNetCore.Builder.EndpointRoutingApplicationBuilderExtensions.UseRouting(Microsoft.AspNetCore.Builder.IApplicationBuilder)" /> call.
    /// </remarks>
    /// <param name="builder">An application's request pipeline builder.</param>
    /// <returns>The <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IApplicationBuilder UseHttpLoggingMiddleware(this IApplicationBuilder builder);
}
