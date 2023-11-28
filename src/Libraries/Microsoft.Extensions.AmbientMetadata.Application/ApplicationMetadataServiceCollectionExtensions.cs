// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for application metadata.
/// </summary>
public static class ApplicationMetadataServiceCollectionExtensions
{
    /// <summary>
    /// Adds an instance of <see cref="ApplicationMetadata"/> to a dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to add the instance to.</param>
    /// <param name="section">The configuration section to bind.</param>
    /// <returns>The value of <paramref name="services"/>>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="section"/> or <paramref name="section"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddApplicationMetadata(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        _ = services.AddOptionsWithValidateOnStart<ApplicationMetadata, ApplicationMetadataValidator>().Bind(section);

        return services;
    }

    /// <summary>
    /// Adds an instance of <see cref="ApplicationMetadata"/> to a dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to add the instance to.</param>
    /// <param name="configure">The delegate to configure <see cref="ApplicationMetadata"/> with.</param>
    /// <returns>The value of <paramref name="services"/>>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddApplicationMetadata(this IServiceCollection services, Action<ApplicationMetadata> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        _ = services.AddOptionsWithValidateOnStart<ApplicationMetadata, ApplicationMetadataValidator>().Configure(configure);

        return services;
    }
}
