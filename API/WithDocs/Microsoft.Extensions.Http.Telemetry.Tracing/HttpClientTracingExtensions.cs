// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Http.Telemetry.Tracing;

/// <summary>
/// Extensions for adding and configuring trace auto collectors for outgoing HTTP requests.
/// </summary>
public static class HttpClientTracingExtensions
{
    /// <summary>
    /// Adds trace auto collector for outgoing HTTP requests.
    /// </summary>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add the tracing auto collector.</param>
    /// <returns>The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">The argument <paramref name="builder" /> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddHttpClientTracing(this TracerProviderBuilder builder);

    /// <summary>
    /// Adds trace auto collector for outgoing HTTP requests.
    /// </summary>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add the tracing auto collector.</param>
    /// <param name="configure">The <see cref="T:Microsoft.Extensions.Http.Telemetry.Tracing.HttpClientTracingOptions" /> configuration delegate.</param>
    /// <returns>The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">The argument <paramref name="builder" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddHttpClientTracing(this TracerProviderBuilder builder, Action<HttpClientTracingOptions> configure);

    /// <summary>
    /// Adds trace auto collector for outgoing HTTP requests.
    /// </summary>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add the tracing auto collector.</param>
    /// <param name="section">Configuration section that contains <see cref="T:Microsoft.Extensions.Http.Telemetry.Tracing.HttpClientTracingOptions" />.</param>
    /// <returns>The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">The argument <paramref name="builder" /> or <paramref name="section" /> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddHttpClientTracing(this TracerProviderBuilder builder, IConfigurationSection section);

    /// <summary>
    /// Adds an enricher that enriches only outgoing HTTP requests traces.
    /// </summary>
    /// <typeparam name="T">Enricher object type.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add this enricher to.</param>
    /// <returns><see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> for chaining.</returns>
    /// <exception cref="T:System.ArgumentNullException">The argument <paramref name="services" /> is <see langword="null" />.</exception>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddHttpClientTraceEnricher<T>(this IServiceCollection services) where T : class, IHttpClientTraceEnricher;

    /// <summary>
    /// Adds an enricher that enriches only outgoing HTTP requests traces.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add this enricher to.</param>
    /// <param name="enricher">Enricher to be added.</param>
    /// <returns><see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> for chaining.</returns>
    /// <exception cref="T:System.ArgumentNullException">The argument <paramref name="services" /> or <paramref name="enricher" /> is <see langword="null" />.</exception>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddHttpClientTraceEnricher(this IServiceCollection services, IHttpClientTraceEnricher enricher);

    /// <summary>
    /// Adds an enricher that enriches only outgoing HTTP requests traces.
    /// </summary>
    /// <typeparam name="T">Enricher object type.</typeparam>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add this enricher to.</param>
    /// <returns><see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> for chaining.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddHttpClientTraceEnricher<T>(this TracerProviderBuilder builder) where T : class, IHttpClientTraceEnricher;

    /// <summary>
    /// Adds an enricher that enriches only outgoing HTTP requests traces.
    /// </summary>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add this enricher.</param>
    /// <param name="enricher">Enricher to be added.</param>
    /// <returns><see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> for chaining.</returns>
    /// <exception cref="T:System.ArgumentNullException">The argument <paramref name="builder" /> or <paramref name="enricher" /> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddHttpClientTraceEnricher(this TracerProviderBuilder builder, IHttpClientTraceEnricher enricher);
}
