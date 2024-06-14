// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for setting up Process enrichers in an <see cref="IServiceCollection" />.
/// </summary>
public static class ProcessEnricherServiceCollectionExtensions
{
    /// <summary>
    /// Adds an instance of the process enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the process enricher to.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddProcessLogEnricher(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        return services
            .AddProcessLogEnricher(_ => { });
    }

    /// <summary>
    /// Adds an instance of the process enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the process enricher to.</param>
    /// <param name="configure">The <see cref="ProcessLogEnricherOptions"/> configuration delegate.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    public static IServiceCollection AddProcessLogEnricher(this IServiceCollection services, Action<ProcessLogEnricherOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return services
            .AddLogEnricher<ProcessLogEnricher>()
            .AddStaticLogEnricher<StaticProcessLogEnricher>()
            .Configure(configure);
    }

    /// <summary>
    /// Adds an instance of the host enricher to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the process enricher to.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="ProcessLogEnricherOptions"/> in the process enricher.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException">Any of the arguments is <see langword="null"/>.</exception>
    public static IServiceCollection AddProcessLogEnricher(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        return services
            .AddLogEnricher<ProcessLogEnricher>()
            .AddStaticLogEnricher<StaticProcessLogEnricher>()
            .Configure<ProcessLogEnricherOptions>(section);
    }
}
