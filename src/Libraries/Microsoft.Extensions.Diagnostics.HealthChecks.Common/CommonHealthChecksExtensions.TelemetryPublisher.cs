// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

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
            .RegisterMetering()
            .AddSingleton<IHealthCheckPublisher, TelemetryHealthCheckPublisher>();

    /// <summary>
    /// Registers a health check publisher which emits telemetry representing the application's health.
    /// </summary>
    /// <param name="services">The dependency injection container to add the publisher to.</param>
    /// <param name="section">Configuration for <see cref="TelemetryHealthCheckPublisherOptions"/>.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services" /> or <paramref name="section"/> are <see langword="null" />.</exception>
    [Experimental]
    public static IServiceCollection AddTelemetryHealthCheckPublisher(this IServiceCollection services, IConfigurationSection section)
        => Throw.IfNull(services)
            .Configure<TelemetryHealthCheckPublisherOptions>(Throw.IfNull(section))
            .RegisterMetering()
            .AddSingleton<IHealthCheckPublisher, TelemetryHealthCheckPublisher>();

    /// <summary>
    /// Registers a health check publisher which emits telemetry representing the application's health.
    /// </summary>
    /// <param name="services">The dependency injection container to add the publisher to.</param>
    /// <param name="configure">Configuration for <see cref="TelemetryHealthCheckPublisherOptions"/>.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services" /> or <paramref name="configure"/> are <see langword="null" />.</exception>
    [Experimental]
    public static IServiceCollection AddTelemetryHealthCheckPublisher(this IServiceCollection services, Action<TelemetryHealthCheckPublisherOptions> configure)
        => Throw.IfNull(services)
            .Configure(Throw.IfNull(configure))
            .RegisterMetering()
            .AddSingleton<IHealthCheckPublisher, TelemetryHealthCheckPublisher>();
}
