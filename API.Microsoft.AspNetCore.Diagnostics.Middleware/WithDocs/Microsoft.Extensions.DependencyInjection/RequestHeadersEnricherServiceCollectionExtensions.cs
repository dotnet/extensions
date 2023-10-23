// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System;
using Microsoft.AspNetCore.Diagnostics.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up Request Headers Log Enricher in an <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
/// </summary>
public static class RequestHeadersEnricherServiceCollectionExtensions
{
    /// <summary>
    /// Adds an instance of Request Headers Log Enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the Request Headers Log Enricher to.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services);

    /// <summary>
    /// Adds an instance of Request Headers Log Enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the Request Headers Log Enricher to.</param>
    /// <param name="configure">The <see cref="T:Microsoft.AspNetCore.Diagnostics.Logging.RequestHeadersLogEnricherOptions" /> configuration delegate.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddRequestHeadersLogEnricher(this IServiceCollection services, Action<RequestHeadersLogEnricherOptions> configure);
}
