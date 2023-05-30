// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// Extension methods for setting up Process enrichers in an <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
/// </summary>
public static class ProcessEnricherExtensions
{
    /// <summary>
    /// Adds an instance of the process enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the process enricher to.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddProcessLogEnricher(this IServiceCollection services);

    /// <summary>
    /// Adds an instance of the process enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the process enricher to.</param>
    /// <param name="configure">The <see cref="T:Microsoft.Extensions.Telemetry.Enrichment.ProcessLogEnricherOptions" /> configuration delegate.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static IServiceCollection AddProcessLogEnricher(this IServiceCollection services, Action<ProcessLogEnricherOptions> configure);

    /// <summary>
    /// Adds an instance of the host enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the process enricher to.</param>
    /// <param name="section">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> to use for configuring <see cref="T:Microsoft.Extensions.Telemetry.Enrichment.ProcessLogEnricherOptions" /> in the process enricher.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static IServiceCollection AddProcessLogEnricher(this IServiceCollection services, IConfigurationSection section);
}
