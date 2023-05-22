// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

public static partial class CoreHealthChecksExtensions
{
    /// <summary>
    /// Registers a health status publisher which opens a TCP port if the application is considered healthy.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the publisher to.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddKubernetesHealthCheckPublisher(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        return services.AddSingleton<IHealthCheckPublisher, KubernetesHealthCheckPublisher>();
    }

    /// <summary>
    /// Registers a health status publisher which opens a TCP port if the application is considered healthy.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the publisher to.</param>
    /// <param name="section">Configuration for <see cref="KubernetesHealthCheckPublisherOptions"/>.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="services" /> or <paramref name="section"/> are <see langword="null" />.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(KubernetesHealthCheckPublisherOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    public static IServiceCollection AddKubernetesHealthCheckPublisher(
        this IServiceCollection services,
        IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        return services
            .Configure<KubernetesHealthCheckPublisherOptions>(section)
            .AddKubernetesHealthCheckPublisher();
    }

    /// <summary>
    /// Registers a health status publisher which opens a TCP port if the application is considered healthy.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the publisher to.</param>
    /// <param name="configure">Configuration for <see cref="KubernetesHealthCheckPublisherOptions"/>.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="services" /> or <paramref name="configure"/> are <see langword="null" />.</exception>
    public static IServiceCollection AddKubernetesHealthCheckPublisher(
        this IServiceCollection services,
        Action<KubernetesHealthCheckPublisherOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return services
            .Configure(configure)
            .AddKubernetesHealthCheckPublisher();
    }
}
