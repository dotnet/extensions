// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// Extension methods for setting up the service enrichers in an <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
/// </summary>
public static class ServiceEnricherExtensions
{
    /// <summary>
    /// Adds an instance of the service enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service enricher to.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddServiceLogEnricher(this IServiceCollection services);

    /// <summary>
    /// Adds an instance of the service enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service enricher to.</param>
    /// <param name="configure">The <see cref="T:Microsoft.Extensions.Telemetry.Enrichment.ServiceLogEnricherOptions" /> configuration delegate.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static IServiceCollection AddServiceLogEnricher(this IServiceCollection services, Action<ServiceLogEnricherOptions> configure);

    /// <summary>
    /// Adds an instance of the service enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service enricher to.</param>
    /// <param name="section">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> to use for configuring <see cref="T:Microsoft.Extensions.Telemetry.Enrichment.ServiceLogEnricherOptions" /> in the service enricher.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static IServiceCollection AddServiceLogEnricher(this IServiceCollection services, IConfigurationSection section);

    /// <summary>
    /// Adds an instance of the service enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service enricher to.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddServiceMetricEnricher(this IServiceCollection services);

    /// <summary>
    /// Adds an instance of the service enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service enricher to.</param>
    /// <param name="configure">The <see cref="T:Microsoft.Extensions.Telemetry.Enrichment.ServiceMetricEnricherOptions" /> configuration delegate.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static IServiceCollection AddServiceMetricEnricher(this IServiceCollection services, Action<ServiceMetricEnricherOptions> configure);

    /// <summary>
    /// Adds an instance of the service enricher to the <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service enricher to.</param>
    /// <param name="section">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> to use for configuring <see cref="T:Microsoft.Extensions.Telemetry.Enrichment.ServiceMetricEnricherOptions" /> in the service enricher.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static IServiceCollection AddServiceMetricEnricher(this IServiceCollection services, IConfigurationSection section);

    /// <summary>
    /// Adds an instance of service trace enricher to the <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add the service trace enricher to.</param>
    /// <returns>The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddServiceTraceEnricher(this TracerProviderBuilder builder);

    /// <summary>
    /// Adds an instance of Service trace enricher to the <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add the service trace enricher to.</param>
    /// <param name="configure">The <see cref="T:Microsoft.Extensions.Telemetry.Enrichment.ServiceTraceEnricherOptions" /> configuration delegate.</param>
    /// <returns>The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddServiceTraceEnricher(this TracerProviderBuilder builder, Action<ServiceTraceEnricherOptions> configure);

    /// <summary>
    /// Adds an instance of Service trace enricher to the <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add the Service trace enricher to.</param>
    /// <param name="section">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> to use for configuring <see cref="T:Microsoft.Extensions.Telemetry.Enrichment.ServiceTraceEnricherOptions" /> in the Service trace enricher.</param>
    /// <returns>The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddServiceTraceEnricher(this TracerProviderBuilder builder, IConfigurationSection section);
}
