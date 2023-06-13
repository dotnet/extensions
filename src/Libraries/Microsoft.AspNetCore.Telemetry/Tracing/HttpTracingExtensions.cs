// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Telemetry.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Shared.Diagnostics;
using OpenTelemetry.Instrumentation.AspNetCore;
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
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add the tracing auto collector.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    public static TracerProviderBuilder AddHttpTracing(this TracerProviderBuilder builder)
    {
        _ = Throw.IfNull(builder);

        return builder
            .ConfigureServices(services => services
                .AddValidatedOptions<HttpTracingOptions, HttpTracingOptionsValidator>())
             .AddHttpTracingInternal();
    }

    /// <summary>
    /// Adds trace auto collector for incoming HTTP requests.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add the tracing auto collector.</param>
    /// <param name="configure">The <see cref="HttpTracingOptions"/> configuration delegate.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    public static TracerProviderBuilder AddHttpTracing(this TracerProviderBuilder builder, Action<HttpTracingOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder
            .ConfigureServices(services => services
                .AddValidatedOptions<HttpTracingOptions, HttpTracingOptionsValidator>()
                .Configure(configure))
             .AddHttpTracingInternal();
    }

    /// <summary>
    /// Adds trace auto collector for incoming HTTP requests.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add the tracing auto collector.</param>
    /// <param name="section">Configuration section that contains <see cref="HttpTracingOptions"/>.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(HttpTracingOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static TracerProviderBuilder AddHttpTracing(this TracerProviderBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        return builder
            .ConfigureServices(services => services
                .AddValidatedOptions<HttpTracingOptions, HttpTracingOptionsValidator>()
                .Bind(section))
             .AddHttpTracingInternal();
    }

    /// <summary>
    /// Adds an enricher that enriches only incoming HTTP requests traces.
    /// </summary>
    /// <typeparam name="T">Enricher object type.</typeparam>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add this enricher.</param>
    /// <returns><see cref="TracerProviderBuilder"/> for chaining.</returns>
    public static TracerProviderBuilder AddHttpTraceEnricher<T>(this TracerProviderBuilder builder)
        where T : class, IHttpTraceEnricher
    {
        _ = Throw.IfNull(builder);

        return builder.ConfigureServices(services => services
            .AddHttpTraceEnricher<T>());
    }

    /// <summary>
    /// Adds an enricher that enriches only incoming HTTP requests traces.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add this enricher.</param>
    /// <param name="enricher">Enricher to be added.</param>
    /// <returns><see cref="TracerProviderBuilder"/> for chaining.</returns>
    public static TracerProviderBuilder AddHttpTraceEnricher(this TracerProviderBuilder builder, IHttpTraceEnricher enricher)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(enricher);

        return builder.ConfigureServices(services => services
                .AddHttpTraceEnricher(enricher));
    }

    /// <summary>
    /// Adds an enricher that enriches only incoming HTTP requests traces.
    /// </summary>
    /// <typeparam name="T">Enricher object type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add this enricher.</param>
    /// <returns><see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="services"/> is <see langword="null"/>.</exception>
    [Experimental]
    public static IServiceCollection AddHttpTraceEnricher<T>(this IServiceCollection services)
        where T : class, IHttpTraceEnricher
    {
        _ = Throw.IfNull(services);

        return services.AddSingleton<IHttpTraceEnricher, T>();
    }

    /// <summary>
    /// Adds an enricher that enriches only incoming HTTP requests traces.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add this enricher.</param>
    /// <param name="enricher">Enricher to be added.</param>
    /// <returns><see cref="TracerProviderBuilder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="services"/> or <paramref name="enricher"/> is <see langword="null" />.</exception>
    [Experimental]
    public static IServiceCollection AddHttpTraceEnricher(this IServiceCollection services, IHttpTraceEnricher enricher)
    {
        _ = Throw.IfNull(services);

        return services.AddSingleton(enricher);
    }

    private static TracerProviderBuilder AddHttpTracingInternal(this TracerProviderBuilder builder)
    {
        _ = builder
            .ConfigureServices(services =>
            {
                _ = services.AddHttpRouteProcessor();
                _ = services.AddHttpRouteUtilities();
                _ = services.AddSingleton<IConfigureOptions<AspNetCoreInstrumentationOptions>, ConfigureAspNetCoreInstrumentationOptions>();

                services.TryAddActivatedSingleton<HttpTraceEnrichmentProcessor>();
                services.TryAddActivatedSingleton<HttpUrlRedactionProcessor>();
            })
            .AddAspNetCoreInstrumentation();

#if !NETCOREAPP3_1_OR_GREATER
        _ = builder.AddProcessor<HttpTraceEnrichmentProcessor>();
#endif
        return builder;
    }
}
