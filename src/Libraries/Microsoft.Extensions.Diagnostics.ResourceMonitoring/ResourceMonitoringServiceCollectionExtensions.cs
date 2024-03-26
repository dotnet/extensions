// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
#if !NETFRAMEWORK
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux;
#endif
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Network;
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

    private static ResourceMonitorBuilder AddWindowsProvider(this ResourceMonitorBuilder builder)
    {
        builder.PickWindowsSnapshotProvider();

        _ = builder.Services
            .AddActivatedSingleton<WindowsCounters>();

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
    private static ResourceMonitorBuilder AddLinuxProvider(this ResourceMonitorBuilder builder)
    {
        _ = Throw.IfNull(builder);

        builder.Services.TryAddActivatedSingleton<ISnapshotProvider, LinuxUtilizationProvider>();

        builder.Services.TryAddSingleton<IFileSystem, OSFileSystem>();
        builder.Services.TryAddSingleton<IUserHz, UserHz>();

        bool injectParserV2 = GetCgroupType();

        if (injectParserV2)
        {
            builder.Services.TryAddSingleton<ILinuxUtilizationParser, LinuxUtilizationParserCgroupV2>();
        }
        else
        {
            builder.Services.TryAddSingleton<ILinuxUtilizationParser, LinuxUtilizationParser>();
        }

        return builder;
    }

    private static bool GetCgroupType()
    {
        DriveInfo[] allDrives = DriveInfo.GetDrives();
        var injectParserV2 = false;
        const string CgroupVersion = "cgroup2fs";
        const string UnifiedCgroupPath = "/sys/fs/cgroup/unified";

        // We check which cgroup version is mounted in the system and based on that we inject the parser.
        foreach (DriveInfo d in allDrives)
        {
            // Currently there are some OS'es which mount both cgroup v1 and v2. And v2 is mounted under /sys/fs/cgroup/unified
            // So, we are checking for the unified cgroup and fallback to cgroup v1, because the path for cgroup v2 is different.
            // This is mostly to support WSL/WSL2, where both cgroup v1 and v2 are mounted and make debugging for Linux easier in VS.
            // https://systemd.io/CGROUP_DELEGATION/#three-different-tree-setups
            if (d.DriveType == DriveType.Ram && d.DriveFormat == CgroupVersion && d.VolumeLabel == UnifiedCgroupPath)
            {
                injectParserV2 = false;
                break;
            }

            if (d.DriveType == DriveType.Ram && d.DriveFormat == CgroupVersion)
            {
                injectParserV2 = true;
                break;
            }
        }

        return injectParserV2;
    }
#endif
}
