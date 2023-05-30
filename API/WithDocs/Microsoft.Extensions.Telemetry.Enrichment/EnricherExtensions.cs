// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// Lets you register telemetry enrichers in a dependency injection container.
/// </summary>
public static class EnricherExtensions
{
    /// <summary>
    /// Registers a log enricher type.
    /// </summary>
    /// <param name="services">The dependency injection container to add the enricher type to.</param>
    /// <typeparam name="T">Enricher type.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddLogEnricher<T>(this IServiceCollection services) where T : class, ILogEnricher;

    /// <summary>
    /// Registers a log enricher instance.
    /// </summary>
    /// <param name="services">The dependency injection container to add the enricher instance to.</param>
    /// <param name="enricher">The enricher instance to add.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> or <paramref name="enricher" /> are <see langword="null" />.</exception>
    public static IServiceCollection AddLogEnricher(this IServiceCollection services, ILogEnricher enricher);

    /// <summary>
    /// Registers a metric enricher type.
    /// </summary>
    /// <typeparam name="T">Enricher type.</typeparam>
    /// <param name="services">The dependency injection container to add the enricher type to.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddMetricEnricher<T>(this IServiceCollection services) where T : class, IMetricEnricher;

    /// <summary>
    /// Registers a metric enricher instance.
    /// </summary>
    /// <param name="services">The dependency injection container to add the enricher instance to.</param>
    /// <param name="enricher">The enricher instance to add.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> or <paramref name="enricher" /> are <see langword="null" />.</exception>
    public static IServiceCollection AddMetricEnricher(this IServiceCollection services, IMetricEnricher enricher);
}
