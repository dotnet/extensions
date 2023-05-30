// Assembly 'Microsoft.Extensions.Telemetry'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// Extension methods for Tracing.
/// </summary>
public static class TracingEnricherExtensions
{
    /// <summary>
    /// Adds an enricher to enrich all traces.
    /// </summary>
    /// <typeparam name="T">Enricher object type.</typeparam>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add enricher.</param>
    /// <returns>The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">The argument <paramref name="builder" /> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddTraceEnricher<T>(this TracerProviderBuilder builder) where T : class, ITraceEnricher;

    /// <summary>
    /// Adds an enricher to enrich all traces.
    /// </summary>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add enricher.</param>
    /// <param name="enricher">The enricher to be added for enriching traces.</param>
    /// <returns>The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">The argument <paramref name="builder" /> or <paramref name="enricher" /> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddTraceEnricher(this TracerProviderBuilder builder, ITraceEnricher enricher);

    /// <summary>
    /// Adds an enricher to enrich all traces.
    /// </summary>
    /// <typeparam name="T">Enricher object type.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add this enricher to.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">The argument <paramref name="services" /> is <see langword="null" />.</exception>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddTraceEnricher<T>(this IServiceCollection services) where T : class, ITraceEnricher;

    /// <summary>
    /// Adds an enricher to enrich all traces.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add this enricher to.</param>
    /// <param name="enricher">Enricher to be added.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">The argument <paramref name="services" /> or <paramref name="enricher" /> is <see langword="null" />.</exception>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddTraceEnricher(this IServiceCollection services, ITraceEnricher enricher);
}
