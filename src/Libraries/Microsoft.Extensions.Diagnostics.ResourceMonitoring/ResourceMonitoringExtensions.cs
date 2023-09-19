// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Extensions.Options;
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
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddResourceMonitoring(
        this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        return services.AddResourceMonitoringInternal(o => { });
    }

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

        return services.AddResourceMonitoringInternal(configure);
    }

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

        return builder.ConfigureMonitorInternal(optionsBuilder => optionsBuilder.Bind(section));
    }

    private static IServiceCollection AddResourceMonitoringInternal(
        this IServiceCollection services,
        Action<IResourceMonitorBuilder> configure)
    {
        var builder = new ResourceMonitorBuilder(services);

#if NETFRAMEWORK
        _ = builder.AddWindowsProvider();
#else
        if (GetPlatform() == PlatformID.Win32NT)
        {
            _ = builder.AddWindowsProvider();
        }
        else
        {
            _ = builder.AddLinuxProvider();
        }
#endif

        configure.Invoke(builder);

        return services;
    }

#if !NETFRAMEWORK
    [ExcludeFromCodeCoverage]
    private static PlatformID GetPlatform()
    {
        var os = Environment.OSVersion;
        if (os.Platform != PlatformID.Win32NT && os.Platform != PlatformID.Unix)
        {
            throw new NotSupportedException("Resource monitoring is not supported on this operating system.");
        }

        return os.Platform;
    }
#endif

    private static ResourceMonitorBuilder AddWindowsProvider(this ResourceMonitorBuilder builder)
    {
        builder.PickWindowsSnapshotProvider();

        _ = builder.Services
            .AddActivatedSingleton<WindowsCounters>()
            .RegisterMetrics();

        _ = builder.Services
            .AddActivatedSingleton<TcpTableInfo>();

        return builder;
    }

    [ExcludeFromCodeCoverage]
    private static void PickWindowsSnapshotProvider(this ResourceMonitorBuilder builder)
    {
        if (JobObjectInfo.SafeJobHandle.IsProcessInJob())
        {
            builder.Services.TryAddSingleton<ISnapshotProvider, WindowsContainerSnapshotProvider>();
        }
        else
        {
            builder.Services.TryAddSingleton<ISnapshotProvider, WindowsSnapshotProvider>();
        }
    }

#if !NETFRAMEWORK
    private static IResourceMonitorBuilder AddLinuxProvider(this IResourceMonitorBuilder builder)
    {
        _ = Throw.IfNull(builder);

        builder.Services
             .RegisterMetrics()
             .TryAddActivatedSingleton<ISnapshotProvider, LinuxUtilizationProvider>();

        builder.Services.TryAddSingleton<IFileSystem, OSFileSystem>();
        builder.Services.TryAddSingleton<IUserHz, UserHz>();
        builder.Services.TryAddSingleton<LinuxUtilizationParser>();

        return builder;
    }

#endif

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
