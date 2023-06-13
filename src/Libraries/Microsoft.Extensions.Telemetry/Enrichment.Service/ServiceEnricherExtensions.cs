// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.Diagnostics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// Extension methods for setting up the service enrichers in an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceEnricherExtensions
{
    /// <summary>
    /// Adds an instance of the service enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service enricher to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddServiceLogEnricher(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        return services
            .AddServiceLogEnricher(_ => { });
    }

    /// <summary>
    /// Adds an instance of the service enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service enricher to.</param>
    /// <param name="configure">The <see cref="ServiceLogEnricherOptions"/> configuration delegate.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    public static IServiceCollection AddServiceLogEnricher(this IServiceCollection services, Action<ServiceLogEnricherOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return services
            .AddLogEnricher<ServiceLogEnricher>()
            .AddLogEnricherOptions(configure);
    }

    /// <summary>
    /// Adds an instance of the service enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service enricher to.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="ServiceLogEnricherOptions"/> in the service enricher.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    public static IServiceCollection AddServiceLogEnricher(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        return services
            .AddLogEnricher<ServiceLogEnricher>()
            .AddLogEnricherOptions(_ => { }, section);
    }

    /// <summary>
    /// Adds an instance of the service enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service enricher to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddServiceMetricEnricher(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        return services
            .AddServiceMetricEnricher(_ => { });
    }

    /// <summary>
    /// Adds an instance of the service enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service enricher to.</param>
    /// <param name="configure">The <see cref="ServiceMetricEnricherOptions"/> configuration delegate.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    public static IServiceCollection AddServiceMetricEnricher(this IServiceCollection services, Action<ServiceMetricEnricherOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return services
            .AddMetricEnricherOptions(configure)
            .AddMetricEnricher<ServiceMetricEnricher>();
    }

    /// <summary>
    /// Adds an instance of the service enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service enricher to.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="ServiceMetricEnricherOptions"/> in the service enricher.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    public static IServiceCollection AddServiceMetricEnricher(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        return services
            .AddMetricEnricherOptions(_ => { }, section)
            .AddMetricEnricher<ServiceMetricEnricher>();
    }

    /// <summary>
    /// Adds an instance of service trace enricher to the <see cref="TracerProviderBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add the service trace enricher to.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static TracerProviderBuilder AddServiceTraceEnricher(this TracerProviderBuilder builder)
    {
        _ = Throw.IfNull(builder);

        _ = builder.AddTraceEnricher<ServiceTraceEnricher>();
        _ = builder.ConfigureServices(services => services.AddTraceEnricherOptions(_ => { }));

        return builder;
    }

    /// <summary>
    /// Adds an instance of Service trace enricher to the <see cref="TracerProviderBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add the service trace enricher to.</param>
    /// <param name="configure">The <see cref="ServiceTraceEnricherOptions"/> configuration delegate.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    public static TracerProviderBuilder AddServiceTraceEnricher(this TracerProviderBuilder builder, Action<ServiceTraceEnricherOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.AddTraceEnricher<ServiceTraceEnricher>();
        _ = builder.ConfigureServices(services => services.AddTraceEnricherOptions(configure));

        return builder;
    }

    /// <summary>
    /// Adds an instance of Service trace enricher to the <see cref="TracerProviderBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add the Service trace enricher to.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="ServiceTraceEnricherOptions"/> in the Service trace enricher.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    public static TracerProviderBuilder AddServiceTraceEnricher(this TracerProviderBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        _ = builder.AddTraceEnricher<ServiceTraceEnricher>();
        _ = builder.ConfigureServices(services => services.AddTraceEnricherOptions(_ => { }, section));

        return builder;
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ServiceMetricEnricherOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    private static IServiceCollection AddMetricEnricherOptions(
        this IServiceCollection services,
        Action<ServiceMetricEnricherOptions> configure,
        IConfigurationSection? section = null)
    {
        _ = services.Configure(configure);

        if (section is not null)
        {
            _ = services.Configure<ServiceMetricEnricherOptions>(section);
        }

        return services;
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ServiceLogEnricherOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    private static IServiceCollection AddLogEnricherOptions(
        this IServiceCollection services,
        Action<ServiceLogEnricherOptions> configure,
        IConfigurationSection? section = null)
    {
        _ = services.Configure(configure);

        if (section is not null)
        {
            _ = services.Configure<ServiceLogEnricherOptions>(section);
        }

        return services;
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ServiceTraceEnricherOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    private static IServiceCollection AddTraceEnricherOptions(
        this IServiceCollection services,
        Action<ServiceTraceEnricherOptions> configure,
        IConfigurationSection? section = null)
    {
        _ = services.Configure(configure);

        if (section is not null)
        {
            _ = services.Configure<ServiceTraceEnricherOptions>(section);
        }

        return services;
    }
}
