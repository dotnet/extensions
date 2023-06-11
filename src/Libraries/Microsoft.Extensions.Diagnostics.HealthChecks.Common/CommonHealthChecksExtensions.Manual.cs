// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

public static partial class CommonHealthChecksExtensions
{
    /// <summary>
    /// Registers a health check provider that enables manual control of the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> or <paramref name="tags"/> are <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddManualHealthCheck(this IHealthChecksBuilder builder, params string[] tags)
        => Throw.IfNull(builder)
            .AddManualHealthCheckDependencies()
            .AddCheck<ManualHealthCheckService>("Manual", tags: Throw.IfNull(tags));

    /// <summary>
    /// Registers a health check provider that enables manual control of the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> or <paramref name="tags"/> are <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddManualHealthCheck(this IHealthChecksBuilder builder, IEnumerable<string> tags)
        => Throw.IfNull(builder)
            .AddManualHealthCheckDependencies()
            .AddCheck<ManualHealthCheckService>("Manual", tags: Throw.IfNull(tags));

    /// <summary>
    /// Sets the manual health check to the healthy state.
    /// </summary>
    /// <param name="manualHealthCheck">The <see cref="IManualHealthCheck"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="manualHealthCheck" /> is <see langword="null" />.</exception>
    public static void ReportHealthy(this IManualHealthCheck manualHealthCheck)
        => Throw.IfNull(manualHealthCheck).Result = HealthCheckResult.Healthy();

    /// <summary>
    /// Sets the manual health check to return an unhealthy states and an associated reason.
    /// </summary>
    /// <param name="manualHealthCheck">The <see cref="IManualHealthCheck"/>.</param>
    /// <param name="reason">The reason why the health check is unhealthy.</param>
    /// <exception cref="ArgumentNullException"><paramref name="manualHealthCheck" /> is <see langword="null" />.</exception>
    public static void ReportUnhealthy(this IManualHealthCheck manualHealthCheck, string reason)
        => Throw.IfNull(manualHealthCheck).Result = HealthCheckResult.Unhealthy(Throw.IfNullOrWhitespace(reason));

    private static IHealthChecksBuilder AddManualHealthCheckDependencies(this IHealthChecksBuilder builder)
        => builder.Services
            .AddSingleton<ManualHealthCheckTracker>()
            .AddTransient(typeof(IManualHealthCheck<>), typeof(ManualHealthCheck<>))
            .AddHealthChecks();
}
