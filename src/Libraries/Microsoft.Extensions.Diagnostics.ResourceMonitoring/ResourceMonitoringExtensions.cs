// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Lets you configure and register resource monitoring components.
/// </summary>
public static class ResourceMonitoringExtensions
{
    /// <summary>
    /// Configures and adds an <see cref="IResourceMonitor"/> implementation to a service collection.
    /// </summary>
    /// <param name="services">The dependency injection container to add the monitor to.</param>
    /// <param name="configure">Delegate to configure <see cref="IResourceMonitorBuilder"/>.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="ArgumentNullException">Either <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddResourceMonitoring(
        this IServiceCollection services,
        Action<IResourceMonitorBuilder> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return services.AddResourceUtilizationInternal(configure);
    }

    /// <summary>
    /// Configures and adds an <see cref="IResourceMonitor"/> implementation to a host.
    /// </summary>
    /// <param name="builder">The host builder to bind to.</param>
    /// <param name="configure">Delegate to configure <see cref="IResourceMonitorBuilder"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException">Either <paramref name="builder"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IHostBuilder ConfigureResourceMonitoring(
        this IHostBuilder builder,
        Action<IResourceMonitorBuilder> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder
            .ConfigureServices((_, services) =>
                services.AddResourceUtilizationInternal(configure));
    }

    /// <summary>
    /// Configures the resource utilization tracker.
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

        return builder.ConfigureTrackerInternal(optionsBuilder => optionsBuilder.Configure(configure));
    }

    /// <summary>
    /// Configures the resource utilization tracker.
    /// </summary>
    /// <param name="builder">The builder instance used to configure the tracker.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="ResourceMonitoringOptions"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException">Either <paramref name="builder"/> or <paramref name="section"/> is <see langword="null" />.</exception>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IResourceMonitorBuilder ConfigureMonitor(
        this IResourceMonitorBuilder builder,
        IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        return builder.ConfigureTrackerInternal(configure: optionsBuilder => optionsBuilder.Bind(section));
    }

    private static IServiceCollection AddResourceUtilizationInternal(
        this IServiceCollection services,
        Action<IResourceMonitorBuilder> configure)
    {
        configure.Invoke(new ResourceUtilizationBuilder(services));
        return services;
    }

    private static IResourceMonitorBuilder ConfigureTrackerInternal(
        this IResourceMonitorBuilder builder,
        Action<OptionsBuilder<ResourceMonitoringOptions>> configure)
    {
        var optionsBuilder = builder
            .Services.AddValidatedOptions<ResourceMonitoringOptions, ResourceUtilizationTrackerOptionsValidator>()
            .Services.AddValidatedOptions<ResourceMonitoringOptions, ResourceUtilizationTrackerOptionsManualValidator>();

        configure.Invoke(optionsBuilder);
        return builder;
    }
}
