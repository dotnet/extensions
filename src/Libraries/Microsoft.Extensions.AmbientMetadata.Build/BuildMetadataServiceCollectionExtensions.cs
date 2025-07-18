// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for Build metadata.
/// </summary>
public static class BuildMetadataServiceCollectionExtensions
{
    /// <summary>
    /// Adds an instance of <see cref="BuildMetadata"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="section">The configuration section to bind the instance of <see cref="BuildMetadata"/> against.</param>
    /// <returns>The <see cref="IServiceCollection"/> for call chaining.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="services"/> or <paramref name="section"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddBuildMetadata(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        _ = services
            .AddOptions<BuildMetadata>()
            .Bind(section);

        return services;
    }

    /// <summary>
    /// Adds an instance of <see cref="BuildMetadata"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">The delegate to configure <see cref="BuildMetadata"/> with.</param>
    /// <returns>The <see cref="IServiceCollection"/> for call chaining.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="services"/> or <paramref name="configure"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddBuildMetadata(this IServiceCollection services, Action<BuildMetadata> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        _ = services
            .AddOptions<BuildMetadata>()
            .Configure(configure);

        return services;
    }
}
