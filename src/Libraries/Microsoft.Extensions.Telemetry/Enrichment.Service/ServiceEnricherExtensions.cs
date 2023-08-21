// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// Provides extension methods for setting up the service enrichers in an <see cref="IServiceCollection" />.
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
            .AddStaticLogEnricher<ServiceLogEnricher>()
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
            .AddStaticLogEnricher<ServiceLogEnricher>()
            .AddLogEnricherOptions(_ => { }, section);
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
}
