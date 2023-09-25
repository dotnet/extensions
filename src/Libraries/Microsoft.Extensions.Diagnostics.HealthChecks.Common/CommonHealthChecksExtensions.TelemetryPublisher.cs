// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CommonHealthChecksExtensions
{
    /// <summary>
    /// Registers a health check publisher which emits telemetry representing the application's health.
    /// </summary>
    /// <param name="services">The dependency injection container to add the publisher to.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddTelemetryHealthCheckPublisher(this IServiceCollection services)
        => Throw.IfNull(services)
            .AddMetrics()
            .AddSingleton<HealthCheckMetrics>()
            .AddSingleton<IHealthCheckPublisher, TelemetryHealthCheckPublisher>();

    /// <summary>
    /// Registers a health check publisher which emits telemetry representing the application's health.
    /// </summary>
    /// <param name="services">The dependency injection container to add the publisher to.</param>
    /// <param name="section">Configuration for <see cref="TelemetryHealthCheckPublisherOptions"/>.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services" /> or <paramref name="section"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddTelemetryHealthCheckPublisher(this IServiceCollection services, IConfigurationSection section)
        => Throw.IfNull(services)
            .Configure<TelemetryHealthCheckPublisherOptions>(Throw.IfNull(section))
            .AddMetrics()
            .AddSingleton<HealthCheckMetrics>()
            .AddSingleton<IHealthCheckPublisher, TelemetryHealthCheckPublisher>();

    /// <summary>
    /// Registers a health check publisher which emits telemetry representing the application's health.
    /// </summary>
    /// <param name="services">The dependency injection container to add the publisher to.</param>
    /// <param name="configure">Configuration for <see cref="TelemetryHealthCheckPublisherOptions"/>.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services" /> or <paramref name="configure"/> is <see langword="null" />.</exception>
    public static IServiceCollection AddTelemetryHealthCheckPublisher(this IServiceCollection services, Action<TelemetryHealthCheckPublisherOptions> configure)
        => Throw.IfNull(services)
            .Configure(Throw.IfNull(configure))
            .AddMetrics()
            .AddSingleton<HealthCheckMetrics>()
            .AddSingleton<IHealthCheckPublisher, TelemetryHealthCheckPublisher>();
}
