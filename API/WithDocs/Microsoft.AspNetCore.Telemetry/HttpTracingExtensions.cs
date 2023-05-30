// Assembly 'Microsoft.AspNetCore.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Extensions for adding and configuring trace auto collectors for incoming HTTP requests.
/// </summary>
public static class HttpTracingExtensions
{
    /// <summary>
    /// Adds trace auto collector for incoming HTTP requests.
    /// </summary>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add the tracing auto collector.</param>
    /// <returns>The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddHttpTracing(this TracerProviderBuilder builder);

    /// <summary>
    /// Adds trace auto collector for incoming HTTP requests.
    /// </summary>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add the tracing auto collector.</param>
    /// <param name="configure">The <see cref="T:Microsoft.AspNetCore.Telemetry.HttpTracingOptions" /> configuration delegate.</param>
    /// <returns>The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddHttpTracing(this TracerProviderBuilder builder, Action<HttpTracingOptions> configure);

    /// <summary>
    /// Adds trace auto collector for incoming HTTP requests.
    /// </summary>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add the tracing auto collector.</param>
    /// <param name="section">Configuration section that contains <see cref="T:Microsoft.AspNetCore.Telemetry.HttpTracingOptions" />.</param>
    /// <returns>The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddHttpTracing(this TracerProviderBuilder builder, IConfigurationSection section);

    /// <summary>
    /// Adds an enricher that enriches only incoming HTTP requests traces.
    /// </summary>
    /// <typeparam name="T">Enricher object type.</typeparam>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add this enricher.</param>
    /// <returns><see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> for chaining.</returns>
    public static TracerProviderBuilder AddHttpTraceEnricher<T>(this TracerProviderBuilder builder) where T : class, IHttpTraceEnricher;

    /// <summary>
    /// Adds an enricher that enriches only incoming HTTP requests traces.
    /// </summary>
    /// <param name="builder">The <see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> to add this enricher.</param>
    /// <param name="enricher">Enricher to be added.</param>
    /// <returns><see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> for chaining.</returns>
    public static TracerProviderBuilder AddHttpTraceEnricher(this TracerProviderBuilder builder, IHttpTraceEnricher enricher);

    /// <summary>
    /// Adds an enricher that enriches only incoming HTTP requests traces.
    /// </summary>
    /// <typeparam name="T">Enricher object type.</typeparam>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add this enricher.</param>
    /// <returns><see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> for chaining.</returns>
    /// <exception cref="T:System.ArgumentNullException">The argument <paramref name="services" /> is <see langword="null" />.</exception>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddHttpTraceEnricher<T>(this IServiceCollection services) where T : class, IHttpTraceEnricher;

    /// <summary>
    /// Adds an enricher that enriches only incoming HTTP requests traces.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add this enricher.</param>
    /// <param name="enricher">Enricher to be added.</param>
    /// <returns><see cref="T:OpenTelemetry.Trace.TracerProviderBuilder" /> for chaining.</returns>
    /// <exception cref="T:System.ArgumentNullException">The argument <paramref name="services" /> or <paramref name="enricher" /> is <see langword="null" />.</exception>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddHttpTraceEnricher(this IServiceCollection services, IHttpTraceEnricher enricher);
}
