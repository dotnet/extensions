// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Extensions for adding the Windows resource utilization provider.
/// </summary>
public static class WindowsUtilizationExtensions
{
    /// <summary>
    /// An extension method to configure and add the default windows utilization provider to services collection.
    /// </summary>
    /// <param name="builder">The tracker builder instance used to add the provider.</param>
    /// <returns>Returns the input tracker builder for call chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null" />.</exception>
    [ExcludeFromCodeCoverage]
    public static IResourceMonitorBuilder AddWindowsProvider(this IResourceMonitorBuilder builder)
    {
        _ = Throw.IfNull(builder);

        if (JobObjectInfo.SafeJobHandle.IsProcessInJob())
        {
            builder.Services.TryAddSingleton<ISnapshotProvider, WindowsContainerSnapshotProvider>();
        }
        else
        {
            builder.Services.TryAddSingleton<ISnapshotProvider, WindowsSnapshotProvider>();
        }

        return builder;
    }

    /// <summary>
    /// An extension method to configure and add the default windows performance counters publisher to services collection.
    /// </summary>
    /// <param name="builder">The tracker builder instance used to add the publisher.</param>
    /// <returns>Returns the input tracker builder for call chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null" />.</exception>
    public static IResourceMonitorBuilder AddWindowsPerfCounterPublisher(this IResourceMonitorBuilder builder)
    {
        _ = Throw.IfNull(builder);
        _ = builder
            .AddWindowsProvider()
            .AddPublisher<WindowsPerfCounterPublisher>();

        return builder;
    }

    /// <summary>
    /// An extension method that creates a few OpenTelemetry instruments for system counters.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null" />.</exception>
    /// <seealso cref="System.Diagnostics.Metrics.Instrument"/>
    [Experimental]
    public static IResourceMonitorBuilder AddWindowsCounters(this IResourceMonitorBuilder builder)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services
            .AddActivatedSingleton<WindowsCounters>();

        _ = builder.Services
            .RegisterMetering();

        _ = builder.Services
            .AddValidatedOptions<WindowsCountersOptions, WindowsCountersOptionsValidator>();

        builder.Services
            .TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<WindowsCountersOptions>, WindowsCountersOptionsCustomValidator>());

        return builder;
    }

    /// <summary>
    /// An extension method that creates a few OpenTelemetry instruments for system counters.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="WindowsCountersOptions"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null" />.</exception>
    /// <seealso cref="System.Diagnostics.Metrics.Instrument"/>
    [Experimental]
    public static IResourceMonitorBuilder AddWindowsCounters(this IResourceMonitorBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        _ = builder.Services
            .AddActivatedSingleton<WindowsCounters>();

        _ = builder.Services
            .RegisterMetering();

        _ = builder.Services
            .AddValidatedOptions<WindowsCountersOptions, WindowsCountersOptionsValidator>()
                .Bind(section);

        builder.Services
            .TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<WindowsCountersOptions>, WindowsCountersOptionsCustomValidator>());

        return builder;
    }

    /// <summary>
    /// An extension method that creates a few OpenTelemetry instruments for system counters.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configure">The delegate for configuration of <see cref="WindowsCountersOptions"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null" />.</exception>
    /// <seealso cref="System.Diagnostics.Metrics.Instrument"/>
    [Experimental]
    public static IResourceMonitorBuilder AddWindowsCounters(this IResourceMonitorBuilder builder, Action<WindowsCountersOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services
            .AddActivatedSingleton<WindowsCounters>();

        _ = builder.Services
            .RegisterMetering();

        _ = builder.Services
            .AddValidatedOptions<WindowsCountersOptions, WindowsCountersOptionsValidator>()
            .Configure(configure);

        builder.Services
            .TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<WindowsCountersOptions>, WindowsCountersOptionsCustomValidator>());

        return builder;
    }
}
