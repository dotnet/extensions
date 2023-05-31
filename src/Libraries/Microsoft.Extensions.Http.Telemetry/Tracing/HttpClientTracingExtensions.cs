// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Telemetry.Tracing.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Shared.Diagnostics;
using OpenTelemetry.Instrumentation.Http;
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
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add the tracing auto collector.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="builder"/> is <see langword="null"/>.</exception>
    public static TracerProviderBuilder AddHttpClientTracing(this TracerProviderBuilder builder)
    {
        _ = Throw.IfNull(builder);

        return builder
            .ConfigureServices(services => services
                .AddValidatedOptions<HttpClientTracingOptions, HttpClientTracingOptionsValidator>())
             .AddHttpClientTracingInternal();
    }

    /// <summary>
    /// Adds trace auto collector for outgoing HTTP requests.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add the tracing auto collector.</param>
    /// <param name="configure">The <see cref="HttpClientTracingOptions"/> configuration delegate.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="builder"/> or <paramref name="configure"/> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddHttpClientTracing(this TracerProviderBuilder builder, Action<HttpClientTracingOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder
            .ConfigureServices(services => services
                .AddValidatedOptions<HttpClientTracingOptions, HttpClientTracingOptionsValidator>()
                .Configure(configure))
             .AddHttpClientTracingInternal();
    }

    /// <summary>
    /// Adds trace auto collector for outgoing HTTP requests.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add the tracing auto collector.</param>
    /// <param name="section">Configuration section that contains <see cref="HttpClientTracingOptions"/>.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="builder"/> or <paramref name="section"/> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddHttpClientTracing(this TracerProviderBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        return builder
            .ConfigureServices(services => services
                .AddValidatedOptions<HttpClientTracingOptions, HttpClientTracingOptionsValidator>()
                .Bind(section))
             .AddHttpClientTracingInternal();
    }

    /// <summary>
    /// Adds an enricher that enriches only outgoing HTTP requests traces.
    /// </summary>
    /// <typeparam name="T">Enricher object type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add this enricher to.</param>
    /// <returns><see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="services"/> is <see langword="null"/>.</exception>
    [Experimental]
    public static IServiceCollection AddHttpClientTraceEnricher<T>(this IServiceCollection services)
        where T : class, IHttpClientTraceEnricher
    {
        _ = Throw.IfNull(services);

        return services.AddSingleton<IHttpClientTraceEnricher, T>();
    }

    /// <summary>
    /// Adds an enricher that enriches only outgoing HTTP requests traces.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add this enricher to.</param>
    /// <param name="enricher">Enricher to be added.</param>
    /// <returns><see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="services"/> or <paramref name="enricher"/> is <see langword="null" />.</exception>
    [Experimental]
    public static IServiceCollection AddHttpClientTraceEnricher(this IServiceCollection services, IHttpClientTraceEnricher enricher)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(enricher);

        return services.AddSingleton(enricher);
    }

    /// <summary>
    /// Adds an enricher that enriches only outgoing HTTP requests traces.
    /// </summary>
    /// <typeparam name="T">Enricher object type.</typeparam>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add this enricher to.</param>
    /// <returns><see cref="TracerProviderBuilder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static TracerProviderBuilder AddHttpClientTraceEnricher<T>(this TracerProviderBuilder builder)
        where T : class, IHttpClientTraceEnricher
    {
        _ = Throw.IfNull(builder);

        return builder.ConfigureServices(services => services
                .AddHttpClientTraceEnricher<T>());
    }

    /// <summary>
    /// Adds an enricher that enriches only outgoing HTTP requests traces.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add this enricher.</param>
    /// <param name="enricher">Enricher to be added.</param>
    /// <returns><see cref="TracerProviderBuilder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="builder"/> or <paramref name="enricher"/> is <see langword="null" />.</exception>
    public static TracerProviderBuilder AddHttpClientTraceEnricher(this TracerProviderBuilder builder, IHttpClientTraceEnricher enricher)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(enricher);

        return builder.ConfigureServices(services => services
                .AddHttpClientTraceEnricher(enricher));
    }

    private static TracerProviderBuilder AddHttpClientTracingInternal(this TracerProviderBuilder builder)
    {
        SelfDiagnostics.EnsureInitialized();

        return builder
            .ConfigureServices(services =>
            {
                _ = services
                    .AddOutgoingRequestContext()
                    .AddHttpRouteProcessor()
                    .AddSingleton<IConfigureOptions<HttpClientInstrumentationOptions>, ConfigureHttpClientInstrumentationOptions>();

                services.TryAddSingleton<IHttpPathRedactor, HttpPathRedactor>();
                services.TryAddActivatedSingleton<HttpClientTraceEnrichmentProcessor>();
                services.TryAddActivatedSingleton<HttpClientRedactionProcessor>();
            })
            .AddHttpClientInstrumentation()
            .AddProcessor<HttpClientTraceEnrichmentProcessor>()
            .AddProcessor<HttpClientRedactionProcessor>();
    }
}
