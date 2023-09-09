// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Metrics;
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

    /// <summary>
    /// Registers a publisher that emits resource utilization information as a Windows performance counter.
    /// </summary>
    /// <param name="builder">The builder instance used to configure the tracker.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null" />.</exception>
    public static IResourceMonitorBuilder AddWindowsPerfCounterPublisher(this IResourceMonitorBuilder builder)
    {
        _ = Throw.IfNull(builder);

        var os = Environment.OSVersion;
        if (os.Platform != PlatformID.Win32NT)
        {
            throw new NotSupportedException("AddWindowsPerfCounterPublisher is only available on Windows.");
        }

        return builder.AddPublisher<WindowsPerfCounterPublisher>();
    }

    private static IServiceCollection AddResourceMonitoringInternal(
        this IServiceCollection services,
        Action<IResourceMonitorBuilder> configure)
    {
        var builder = new ResourceMonitorBuilder(services);

#if NETFRAMEWORK
        _ = builder.AddWindowsProvider();
#elif !NET8_0_OR_GREATER
        var os = Environment.OSVersion;
        if (os.Platform == PlatformID.Win32NT)
        {
            _ = builder.AddWindowsProvider();
        }
        else if (os.Platform == PlatformID.Unix)
        {
            _ = builder.AddLinuxProvider();
        }
        else
        {
            throw new NotSupportedException("Resource monitoring is not supported on this operating system.");
        }
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
            throw new NotSupportedException("Resource monitoring is not supported on this operating system.");
        }
#endif

        configure.Invoke(builder);

        return services;
    }

    private static ResourceMonitorBuilder AddWindowsProvider(this ResourceMonitorBuilder builder)
    {
        if (JobObjectInfo.SafeJobHandle.IsProcessInJob())
        {
            builder.Services.TryAddSingleton<ISnapshotProvider, WindowsContainerSnapshotProvider>();
        }
        else
        {
            builder.Services.TryAddSingleton<ISnapshotProvider, WindowsSnapshotProvider>();
        }

        _ = builder.Services
            .AddActivatedSingleton<WindowsCounters>()
            .RegisterMetrics();

        return builder;
    }

#if !NETFRAMEWORK
    private static IResourceMonitorBuilder AddLinuxProvider(this IResourceMonitorBuilder builder)
    {
        _ = Throw.IfNull(builder);

        builder.Services
             .RegisterMetrics()
             .TryAddActivatedSingleton<ISnapshotProvider, LinuxUtilizationProvider>();

        builder.Services.TryAddSingleton<IFileSystem, OSFileSystem>();
        builder.Services.TryAddSingleton<IOperatingSystem, IsOperatingSystem>();
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
