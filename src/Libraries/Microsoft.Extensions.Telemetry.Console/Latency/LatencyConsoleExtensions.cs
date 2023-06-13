// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Console;

/// <summary>
/// Extensions to add console latency data exporter.
/// </summary>
public static class LatencyConsoleExtensions
{
    /// <summary>
    /// Add latency data exporter for the console.
    /// </summary>
    /// <param name="services">Dependency injection container.</param>
    /// <returns>Provided service collection with <see cref="LatencyConsoleExporter"/> added.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddConsoleLatencyDataExporter(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        _ = services.AddOptions<LarencyConsoleOptions>();
        services.TryAddSingleton<ILatencyDataExporter, LatencyConsoleExporter>();

        return services;
    }

    /// <summary>
    /// Add latency data exporter for the console.
    /// </summary>
    /// <param name="services">Dependency injection container.</param>
    /// <param name="configure"><see cref="LarencyConsoleOptions"/> configuration delegate.</param>
    /// <returns>Provided service collection with <see cref="LatencyConsoleExporter"/> added.</returns>
    /// <exception cref="ArgumentNullException">Either <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddConsoleLatencyDataExporter(this IServiceCollection services, Action<LarencyConsoleOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        _ = services.Configure(configure);

        return AddConsoleLatencyDataExporter(services);
    }

    /// <summary>
    /// Add latency data exporter for the console.
    /// </summary>
    /// <param name="services">Dependency injection container.</param>
    /// <param name="section">Configuration of <see cref="LarencyConsoleOptions"/>.</param>
    /// <returns>Provided service collection with <see cref="LatencyConsoleExporter"/> added.</returns>
    /// <exception cref="ArgumentNullException">Either <paramref name="services"/> or <paramref name="section"/> is <see langword="null"/>.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(LarencyConsoleOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    public static IServiceCollection AddConsoleLatencyDataExporter(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        _ = services.Configure<LarencyConsoleOptions>(section);

        return AddConsoleLatencyDataExporter(services);
    }
}
