// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Controls various health check features.
/// </summary>
public static partial class CoreHealthChecksExtensions
{
    /// <summary>
    /// Registers a health check provider that's tied to the application's lifecycle.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="builder" /> is <see langword="null" />.</exception>
    /// <remarks>The application's lifecycle is tracked through <see cref="IHostApplicationLifetime"/>.</remarks>
    public static IHealthChecksBuilder AddApplicationLifecycleHealthCheck(this IHealthChecksBuilder builder)
    {
        _ = Throw.IfNull(builder);

        return builder.AddCheck<ApplicationLifecycleHealthCheck>("ApplicationLifecycleHealthCheck");
    }

    /// <summary>
    /// Registers a health check provider that's tied to the application's lifecycle.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="builder" /> or <paramref name="tags"/> are <see langword="null" />.</exception>
    /// <remarks>The application's lifecycle is tracked through <see cref="IHostApplicationLifetime"/>.</remarks>
    public static IHealthChecksBuilder AddApplicationLifecycleHealthCheck(this IHealthChecksBuilder builder, IEnumerable<string> tags)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(tags);

        return builder.AddCheck<ApplicationLifecycleHealthCheck>("ApplicationLifecycleHealthCheck", tags: tags);
    }
}
