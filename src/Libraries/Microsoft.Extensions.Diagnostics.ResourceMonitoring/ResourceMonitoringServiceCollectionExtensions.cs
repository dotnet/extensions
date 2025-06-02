// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
#if !NETFRAMEWORK
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Disk;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Network;

#endif
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Disk;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Network;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Lets you configure and register resource monitoring components.
/// </summary>
public static class ResourceMonitoringServiceCollectionExtensions
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
    [Obsolete(DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiMessage,
        DiagnosticId = DiagnosticIds.Obsoletions.NonObservableResourceMonitoringApiDiagId,
        UrlFormat = DiagnosticIds.UrlFormat)]
    public static IServiceCollection AddResourceMonitoring(
        this IServiceCollection services,
        Action<IResourceMonitorBuilder> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return services.AddResourceMonitoringInternal(configure);
    }

    // can't easily test the exception throwing case
    [ExcludeFromCodeCoverage]
    private static IServiceCollection AddResourceMonitoringInternal(
        this IServiceCollection services,
        Action<IResourceMonitorBuilder> configure)
    {
        var builder = new ResourceMonitorBuilder(services);

        _ = services.AddMetrics();

#if NETFRAMEWORK
        _ = builder.AddWindowsProvider();
#else
        if (OperatingSystem.IsWindows())
        {
            _ = builder.AddWindowsProvider();
        }
        else if (OperatingSystem.IsLinux())
        {
            _ = builder.AddLinuxProvider();
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
#endif

        configure.Invoke(builder);

        return services;
    }

    [SupportedOSPlatform("windows")]
    private static ResourceMonitorBuilder AddWindowsProvider(this ResourceMonitorBuilder builder)
    {
        builder.PickWindowsSnapshotProvider();

        _ = builder.Services
            .AddActivatedSingleton<WindowsNetworkMetrics>()
            .AddActivatedSingleton<ITcpStateInfoProvider, WindowsTcpStateInfo>();

        builder.Services.TryAddSingleton(TimeProvider.System);

        _ = builder.Services
            .AddActivatedSingleton<WindowsDiskMetrics>()
            .AddActivatedSingleton<IPerformanceCounterFactory, PerformanceCounterFactory>();

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
    private static ResourceMonitorBuilder AddLinuxProvider(this ResourceMonitorBuilder builder)
    {
        _ = Throw.IfNull(builder);

        builder.Services.TryAddActivatedSingleton<ISnapshotProvider, LinuxUtilizationProvider>();

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.TryAddSingleton<IFileSystem, OSFileSystem>();
        builder.Services.TryAddSingleton<IUserHz, UserHz>();
        builder.PickLinuxParser();

        _ = builder.Services
            .AddActivatedSingleton<LinuxNetworkUtilizationParser>()
            .AddActivatedSingleton<LinuxNetworkMetrics>()
            .AddActivatedSingleton<ITcpStateInfoProvider, LinuxTcpStateInfo>()
            .AddActivatedSingleton<IDiskStatsReader, DiskStatsReader>()
            .AddActivatedSingleton<LinuxSystemDiskMetrics>();

        return builder;
    }

    [ExcludeFromCodeCoverage]
    private static void PickLinuxParser(this ResourceMonitorBuilder builder)
    {
        var injectParserV2 = ResourceMonitoringLinuxCgroupVersion.GetCgroupType();
        if (injectParserV2)
        {
            builder.Services.TryAddSingleton<ILinuxUtilizationParser, LinuxUtilizationParserCgroupV2>();
        }
        else
        {
            builder.Services.TryAddSingleton<ILinuxUtilizationParser, LinuxUtilizationParserCgroupV1>();
        }
    }
#endif
}
