// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Enrichment;

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
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> or <paramref name="enricher" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddLogEnricher(this IServiceCollection services, ILogEnricher enricher);

    /// <summary>
    /// Registers a static log enricher type.
    /// </summary>
    /// <param name="services">The dependency injection container to add the enricher type to.</param>
    /// <typeparam name="T">Enricher type.</typeparam>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddStaticLogEnricher<T>(this IServiceCollection services) where T : class, IStaticLogEnricher;

    /// <summary>
    /// Registers a static log enricher instance.
    /// </summary>
    /// <param name="services">The dependency injection container to add the enricher instance to.</param>
    /// <param name="enricher">The enricher instance to add.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> or <paramref name="enricher" /> is <see langword="null" />.</exception>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddStaticLogEnricher(this IServiceCollection services, IStaticLogEnricher enricher);
}
