// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Extensions for adding the Linux resource utilization provider.
/// </summary>
public static class LinuxUtilizationExtensions
{
    /// <summary>
    /// An extension method to configure and add the Linux utilization provider to services collection.
    /// </summary>
    /// <param name="builder">The tracker builder instance used to add the provider.</param>
    /// <returns>Returns the input tracker builder for call chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null" />.</exception>
    public static IResourceMonitorBuilder AddLinuxProvider(this IResourceMonitorBuilder builder)
    {
        _ = Throw.IfNull(builder);

        builder.Services
             .RegisterMetering()
             .AddValidatedOptions<LinuxResourceUtilizationProviderOptions, LinuxCountersOptionsValidator>()
             .Services.TryAddActivatedSingleton<ISnapshotProvider, LinuxUtilizationProvider>();

        builder.Services.TryAddSingleton<IFileSystem, OSFileSystem>();
        builder.Services.TryAddSingleton<IOperatingSystem, IsOperatingSystem>();
        builder.Services.TryAddSingleton<IUserHz, UserHz>();
        builder.Services.TryAddSingleton<LinuxUtilizationParser>();

        return builder;
    }

    /// <summary>
    /// An extension method to configure and add the Linux utilization provider to services collection.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring of <see cref="LinuxResourceUtilizationProviderOptions"/>.</param>
    /// <returns>Returns the builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null" />.</exception>
    /// <seealso cref="System.Diagnostics.Metrics.Instrument"/>
    public static IResourceMonitorBuilder AddLinuxProvider(this IResourceMonitorBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        _ = builder.Services.AddOptions<LinuxResourceUtilizationProviderOptions>().Bind(section);

        return builder.AddLinuxProvider();
    }

    /// <summary>
    /// An extension method to configure and add the Linux utilization provider to services collection.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configure">The delegate for configuring of <see cref="LinuxResourceUtilizationProviderOptions"/>.</param>
    /// <returns>Returns the builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="configure"/> is <see langword="null" />.</exception>
    public static IResourceMonitorBuilder AddLinuxProvider(this IResourceMonitorBuilder builder, Action<LinuxResourceUtilizationProviderOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services.AddOptions<LinuxResourceUtilizationProviderOptions>().Configure(configure);

        return builder.AddLinuxProvider();
    }
}
