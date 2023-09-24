// Assembly 'Microsoft.Extensions.Diagnostics.Extra'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.Enrichment;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for setting up Process enrichers in an <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
/// </summary>
public static class ProcessEnricherServiceCollectionExtensions
{
    /// <summary>
    /// Adds an instance of the process enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the process enricher to.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddProcessLogEnricher(this IServiceCollection services);

    /// <summary>
    /// Adds an instance of the process enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the process enricher to.</param>
    /// <param name="configure">The <see cref="T:Microsoft.Extensions.Diagnostics.Enrichment.ProcessLogEnricherOptions" /> configuration delegate.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static IServiceCollection AddProcessLogEnricher(this IServiceCollection services, Action<ProcessLogEnricherOptions> configure);

    /// <summary>
    /// Adds an instance of the host enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the process enricher to.</param>
    /// <param name="section">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> to use for configuring <see cref="T:Microsoft.Extensions.Diagnostics.Enrichment.ProcessLogEnricherOptions" /> in the process enricher.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static IServiceCollection AddProcessLogEnricher(this IServiceCollection services, IConfigurationSection section);
}
