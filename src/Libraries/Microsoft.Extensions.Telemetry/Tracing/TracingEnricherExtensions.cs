// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// Provides extension methods for tracing.
/// </summary>
public static class TracingEnricherExtensions
{
    /// <summary>
    /// Adds an enricher to enrich all traces.
    /// </summary>
    /// <typeparam name="T">The enricher object type.</typeparam>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add the enricher.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="builder"/> is <see langword="null"/>.</exception>
    public static TracerProviderBuilder AddTraceEnricher<T>(this TracerProviderBuilder builder)
            where T : class, ITraceEnricher
    {
        _ = Throw.IfNull(builder);

        return builder.ConfigureServices(services => services.AddTraceEnricher<T>());
    }

    /// <summary>
    /// Adds an enricher to enrich all traces.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add enricher.</param>
    /// <param name="enricher">The enricher to be added for enriching traces.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="builder"/> or <paramref name="enricher"/> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddTraceEnricher(this TracerProviderBuilder builder, ITraceEnricher enricher)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(enricher);

        return builder.ConfigureServices(services => services.AddTraceEnricher(enricher));
    }

    /// <summary>
    /// Adds an enricher to enrich all traces.
    /// </summary>
    /// <typeparam name="T">Enricher object type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add this enricher to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="services"/> is <see langword="null"/>.</exception>
    [Experimental(diagnosticId: Experiments.Telemetry, UrlFormat = Experiments.UrlFormat)]
    public static IServiceCollection AddTraceEnricher<T>(this IServiceCollection services)
        where T : class, ITraceEnricher
    {
        _ = Throw.IfNull(services);

        return services
            .AddSingleton<ITraceEnricher, T>()
            .TryAddEnrichmentProcessor();
    }

    /// <summary>
    /// Adds an enricher to enrich all traces.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add this enricher to.</param>
    /// <param name="enricher">Enricher to be added.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="services"/> or <paramref name="enricher"/> is <see langword="null" />.</exception>
    [Experimental(diagnosticId: Experiments.Telemetry, UrlFormat = Experiments.UrlFormat)]
    public static IServiceCollection AddTraceEnricher(this IServiceCollection services, ITraceEnricher enricher)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(enricher);

        return services
            .AddSingleton(enricher)
            .TryAddEnrichmentProcessor();
    }

    private static IServiceCollection TryAddEnrichmentProcessor(this IServiceCollection services)
    {
        // Stryker disable once Linq
        if (!services.Any(x => x.ServiceType == typeof(TraceEnrichmentProcessor)))
        {
            _ = services
                .AddSingleton<TraceEnrichmentProcessor>()
                .ConfigureOpenTelemetryTracerProvider((sp, builder) =>
                {
                    var proc = sp.GetRequiredService<TraceEnrichmentProcessor>();
                    _ = builder.AddProcessor(proc);
                });
        }

        return services;
    }
}
