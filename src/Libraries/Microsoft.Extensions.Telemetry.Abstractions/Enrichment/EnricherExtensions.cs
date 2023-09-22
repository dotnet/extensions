﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Enrichment;

/// <summary>
/// Lets you register telemetry enrichers in a dependency injection container.
/// </summary>
public static class EnricherExtensions
{
    /// <summary>
    /// Registers a log enricher type.
    /// </summary>
    /// <param name="services">The dependency injection container to add the enricher type to.</param>
    /// <typeparam name="T">Enricher type.</typeparam>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddLogEnricher<T>(this IServiceCollection services)
        where T : class, ILogEnricher
    {
        _ = Throw.IfNull(services);

        return services.AddSingleton<ILogEnricher, T>();
    }

    /// <summary>
    /// Registers a log enricher instance.
    /// </summary>
    /// <param name="services">The dependency injection container to add the enricher instance to.</param>
    /// <param name="enricher">The enricher instance to add.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="enricher"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddLogEnricher(this IServiceCollection services, ILogEnricher enricher)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(enricher);

        return services.AddSingleton(enricher);
    }

    /// <summary>
    /// Registers a static log enricher type.
    /// </summary>
    /// <param name="services">The dependency injection container to add the enricher type to.</param>
    /// <typeparam name="T">Enricher type.</typeparam>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddStaticLogEnricher<T>(this IServiceCollection services)
        where T : class, IStaticLogEnricher
    {
        _ = Throw.IfNull(services);

        return services.AddSingleton<IStaticLogEnricher, T>();
    }

    /// <summary>
    /// Registers a static log enricher instance.
    /// </summary>
    /// <param name="services">The dependency injection container to add the enricher instance to.</param>
    /// <param name="enricher">The enricher instance to add.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="enricher"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddStaticLogEnricher(this IServiceCollection services, IStaticLogEnricher enricher)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(enricher);

        return services.AddSingleton(enricher);
    }
}
