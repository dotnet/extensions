// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Extensions.Telemetry.Latency.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Extensions to add latency context.
/// </summary>
public static class LatencyContextExtensions
{
    /// <summary>
    /// Add latency context.
    /// </summary>
    /// <param name="services">Dependency injection container.</param>
    /// <returns>Provided service collection with <see cref="ILatencyContext"/> added.</returns>
    public static IServiceCollection AddLatencyContext(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);
        _ = services.AddOptions<LatencyContextOptions>();
        services.TryAddSingleton<LatencyContextRegistrySet>();
        services.TryAddSingleton<LatencyInstrumentProvider>();
        services.TryAddSingleton<ILatencyContextProvider, LatencyContextProvider>();
        services.TryAddSingleton<ILatencyContextTokenIssuer, LatencyContextTokenIssuer>();
        return services;
    }

    /// <summary>
    /// Add latency context.
    /// </summary>
    /// <param name="services">Dependency injection container.</param>
    /// <param name="configure"><see cref="LatencyContextOptions"/> configuration delegate.</param>
    /// <returns>Provided service collection with <see cref="LatencyContextProvider"/> added.</returns>
    public static IServiceCollection AddLatencyContext(this IServiceCollection services, Action<LatencyContextOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        _ = services.Configure(configure);

        return AddLatencyContext(services);
    }

    /// <summary>
    /// Add latency context.
    /// </summary>
    /// <param name="services">Dependency injection container.</param>
    /// <param name="section">Configuration of <see cref="LatencyContextOptions"/>.</param>
    /// <returns>Provided service collection with <see cref="LatencyContextProvider"/> added.</returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(LatencyContextOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    public static IServiceCollection AddLatencyContext(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        _ = services.Configure<LatencyContextOptions>(section);

        return AddLatencyContext(services);
    }
}
