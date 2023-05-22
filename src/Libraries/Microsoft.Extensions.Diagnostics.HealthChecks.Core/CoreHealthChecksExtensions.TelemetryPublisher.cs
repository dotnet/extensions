// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Telemetry.Metering;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

public static partial class CoreHealthChecksExtensions
{
    /// <summary>
    /// Registers a health status publisher which emits logs and metrics tracking the application's health.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the publisher to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddTelemetryHealthCheckPublisher(this IServiceCollection services)
        => services
            .RegisterMetering()
            .AddSingleton<IHealthCheckPublisher, TelemetryHealthCheckPublisher>();
}
