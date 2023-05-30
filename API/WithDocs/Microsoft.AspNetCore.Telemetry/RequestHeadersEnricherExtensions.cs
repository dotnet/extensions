// Assembly 'Microsoft.AspNetCore.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Extension methods for setting up Request Headers Log Enricher in an <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
/// </summary>
public static class RequestHeadersEnricherExtensions
{
    /// <summary>
    /// Adds an instance of Request Headers Log Enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the Request Headers Log Enricher to.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services);

    /// <summary>
    /// Adds an instance of Request Headers Log Enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the Request Headers Log Enricher to.</param>
    /// <param name="configure">The <see cref="T:Microsoft.AspNetCore.Telemetry.RequestHeadersLogEnricherOptions" /> configuration delegate.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services, Action<RequestHeadersLogEnricherOptions> configure);

    /// <summary>
    /// Adds an instance of Request Headers Log Enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the Request Headers Log Enricher to.</param>
    /// <param name="section">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> to use for configuring <see cref="T:Microsoft.AspNetCore.Telemetry.RequestHeadersLogEnricherOptions" />
    /// in the Request Headers Log Enricher.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services, IConfigurationSection section);
}
