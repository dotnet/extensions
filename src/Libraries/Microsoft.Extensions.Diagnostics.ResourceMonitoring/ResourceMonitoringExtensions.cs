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
/// Lets you configure and register resource utilization components.
/// </summary>
public static class ResourceMonitoringExtensions
{
    /// <summary>
    /// Configures and adds an <see cref="IResourceUtilizationTracker"/> implementation to your service collection.
    /// </summary>
    /// <param name="services">The dependency injection container to add the tracker to.</param>
    /// <param name="configure">Delegate to configure <see cref="IResourceUtilizationTrackerBuilder"/>.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="ArgumentNullException">If either <paramref name="services"/> or <paramref name="configure"/> are <see langword="null"/>.</exception>
    public static IServiceCollection AddResourceUtilization(
        this IServiceCollection services,
        Action<IResourceUtilizationTrackerBuilder> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return services.AddResourceUtilizationInternal(configure);
    }

    /// <summary>
    /// Configures and adds an <see cref="IResourceUtilizationTracker"/> implementation to your host.
    /// </summary>
    /// <param name="builder">The host builder to bind to.</param>
    /// <param name="configure">Delegate to configure <see cref="IResourceUtilizationTrackerBuilder"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException">If either <paramref name="builder"/> or <paramref name="configure"/> are <see langword="null"/>.</exception>
    public static IHostBuilder ConfigureResourceUtilization(
        this IHostBuilder builder,
        Action<IResourceUtilizationTrackerBuilder> configure)
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
    /// <param name="configure">Delegate to configure <see cref="ResourceUtilizationTrackerOptions"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException">If either <paramref name="builder"/> or <paramref name="configure"/> are <see langword="null" />.</exception>
    public static IResourceUtilizationTrackerBuilder ConfigureTracker(
        this IResourceUtilizationTrackerBuilder builder,
        Action<ResourceUtilizationTrackerOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.ConfigureTrackerInternal(optionsBuilder => optionsBuilder.Configure(configure));
    }

    /// <summary>
    /// Configures the resource utilization tracker.
    /// </summary>
    /// <param name="builder">The builder instance used to configure the tracker.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="ResourceUtilizationTrackerOptions"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException">If either <paramref name="builder"/> or <paramref name="section"/> are <see langword="null" />.</exception>
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IResourceUtilizationTrackerBuilder ConfigureTracker(
        this IResourceUtilizationTrackerBuilder builder,
        IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        return builder.ConfigureTrackerInternal(configure: optionsBuilder => optionsBuilder.Bind(section));
    }

    /// <summary>
    /// Adds Null Resource Utilization services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The DI container to bind to.</param>
    /// <returns>Returns the input container.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="services"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddNullResourceUtilization(this IServiceCollection services)
    {
        services = Throw.IfNull(services);

        return services.AddSingleton<IResourceUtilizationTracker, NullResourceUtilizationTrackerService>();
    }

    /// <summary>
    /// Adds a platform independent and non-operational <see cref="ISnapshotProvider"/> to the service collection.
    /// </summary>
    /// <param name="builder">The builder instance used to configure the tracker.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <remarks>This extension method will add a non-operational provider that generates fixed CPU and Memory information. Don't use this in
    /// production, but you can use it in development environment when you're uncertain about the underlying platform and don't need real data
    /// to be generated.</remarks>
    /// <exception cref="ArgumentNullException">If <paramref name="builder"/> is <see langword="null" />.</exception>
    public static IResourceUtilizationTrackerBuilder AddNullResourceUtilizationProvider(this IResourceUtilizationTrackerBuilder builder)
    {
        _ = Throw.IfNull(builder);

        _ = builder.Services.AddSingleton<ISnapshotProvider, NullSnapshotProvider>();
        return builder;
    }

    private static IServiceCollection AddResourceUtilizationInternal(
        this IServiceCollection services,
        Action<IResourceUtilizationTrackerBuilder> configure)
    {
        configure.Invoke(new ResourceUtilizationBuilder(services));
        return services;
    }

    private static IResourceUtilizationTrackerBuilder ConfigureTrackerInternal(
        this IResourceUtilizationTrackerBuilder builder,
        Action<OptionsBuilder<ResourceUtilizationTrackerOptions>> configure)
    {
        var optionsBuilder = builder
            .Services.AddValidatedOptions<ResourceUtilizationTrackerOptions, ResourceUtilizationTrackerOptionsValidator>()
            .Services.AddValidatedOptions<ResourceUtilizationTrackerOptions, ResourceUtilizationTrackerOptionsManualValidator>();

        configure.Invoke(optionsBuilder);
        return builder;
    }
}
