// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Extensions to configure a latency context.
/// </summary>
public static class LatencyRegistryExtensions
{
    /// <summary>
    /// Registers a set of checkpoint names for a latency context.
    /// </summary>
    /// <param name="services">The dependency injection container to add the names to.</param>
    /// <param name="names">Set of checkpoint names.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="names"/> are <see langword="null"/>.</exception>
    public static IServiceCollection RegisterCheckpointNames(this IServiceCollection services, params string[] names)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(names);

        CheckNames(names);
        services.ConfigureOption(o => o.AddCheckpointNames(names));

        return services;
    }

    /// <summary>
    /// Registers a set of measure names for a latency context.
    /// </summary>
    /// <param name="services">The dependency injection container to add the names to.</param>
    /// <param name="names">Set of measure names.</param>
    /// <returns>Provided service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="names"/> are <see langword="null"/>.</exception>
    public static IServiceCollection RegisterMeasureNames(this IServiceCollection services, params string[] names)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(names);

        CheckNames(names);
        services.ConfigureOption(o => o.AddMeasureNames(names));

        return services;
    }

    /// <summary>
    /// Registers a set of tag names for a latency context.
    /// </summary>
    /// <param name="services">The dependency injection container to add the names to.</param>
    /// <param name="names">Set of tag names.</param>
    /// <returns>Provided service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="names"/> are <see langword="null"/>.</exception>
    public static IServiceCollection RegisterTagNames(this IServiceCollection services, params string[] names)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(names);

        CheckNames(names);
        services.ConfigureOption(o => o.AddTagNames(names));

        return services;
    }

    private static void CheckNames(string[] names)
    {
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Throw.ArgumentException(nameof(names), "Name is either null or whitespace");
            }
        }
    }

    private static void ConfigureOption(this IServiceCollection services, Action<LatencyContextRegistrationOptions> action)
    {
        _ = services.AddOptions<LatencyContextRegistrationOptions>();
        _ = services.Configure(action);
    }
}
