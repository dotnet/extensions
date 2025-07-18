// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for Azure Virtual Machine metadata types.
/// </summary>
public static class AzureVmMetadataServiceCollectionExtensions
{
    /// <summary>
    /// Adds an instance of <see cref="AzureVmMetadata"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="section">The configuration section to bind.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="services"/> or <paramref name="section"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddAzureVmMetadata(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        _ = services
            .AddOptions<AzureVmMetadata>()
            .Bind(section);

        return services;
    }

    /// <summary>
    /// Adds an instance of <see cref="AzureVmMetadata"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">The delegate to configure <see cref="AzureVmMetadata"/> with.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">The argument <paramref name="services"/> or <paramref name="configure"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddAzureVmMetadata(this IServiceCollection services, Action<AzureVmMetadata> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        _ = services
            .AddOptions<AzureVmMetadata>()
            .Configure(configure);

        return services;
    }
}
