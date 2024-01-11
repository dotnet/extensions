// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Lets you configure and register resource monitoring components.
/// </summary>
public static class ResourceMonitoringBuilderExtensions
{
    /// <summary>
    /// Configures the resource monitor.
    /// </summary>
    /// <param name="builder">The builder instance used to configure the tracker.</param>
    /// <param name="configure">Delegate to configure <see cref="ResourceMonitoringOptions"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException">Either <paramref name="builder"/> or <paramref name="configure"/> is <see langword="null" />.</exception>
    public static IResourceMonitorBuilder ConfigureMonitor(
        this IResourceMonitorBuilder builder,
        Action<ResourceMonitoringOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.ConfigureMonitorInternal(optionsBuilder => optionsBuilder.Configure(configure));
    }

    /// <summary>
    /// Configures the resource monitor.
    /// </summary>
    /// <param name="builder">The builder instance used to configure the tracker.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="ResourceMonitoringOptions"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException">Either <paramref name="builder"/> or <paramref name="section"/> is <see langword="null" />.</exception>
    public static IResourceMonitorBuilder ConfigureMonitor(
        this IResourceMonitorBuilder builder,
        IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        return builder.ConfigureMonitorInternal(optionsBuilder => optionsBuilder.Bind(section));
    }

    private static IResourceMonitorBuilder ConfigureMonitorInternal(
        this IResourceMonitorBuilder builder,
        Action<OptionsBuilder<ResourceMonitoringOptions>> configure)
    {
        var optionsBuilder = builder
            .Services.AddOptionsWithValidateOnStart<ResourceMonitoringOptions, ResourceMonitoringOptionsValidator>()
            .Services.AddOptionsWithValidateOnStart<ResourceMonitoringOptions, ResourceMonitoringOptionsCustomValidator>();

        configure.Invoke(optionsBuilder);
        return builder;
    }
}
