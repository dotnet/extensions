// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Diagnostics.Probes;

/// <summary>
/// Extensions for setting up probes.
/// </summary>
public static class ProbeCheckExtensions
{
    private static readonly string[] _livenessTags = new[] { ProbeTags.Liveness };
    private static readonly string[] _startupTags = new[] { ProbeTags.Startup };
    private static readonly string[] _readinessTags = new[] { ProbeTags.Readiness };

    /// <summary>
    /// Adds a health check to the liveness probe.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="IHealthCheck"/> to be added.</typeparam>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">Name for the check. This allows to distinguish it from other health checks.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static IHealthChecksBuilder AddLivenessCheck<T>(this IHealthChecksBuilder builder, string name)
        where T : class, IHealthCheck
    {
        return builder
            .AddCheck<T>(name, tags: _livenessTags);
    }

    /// <summary>
    /// Adds a health check to the startup probe.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="IHealthCheck"/> to be added.</typeparam>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">Name for the check. This allows to distinguish it from other health checks.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static IHealthChecksBuilder AddStartupCheck<T>(this IHealthChecksBuilder builder, string name)
        where T : class, IHealthCheck
    {
        return builder
            .AddCheck<T>(name, tags: _startupTags);
    }

    /// <summary>
    /// Adds a health check to the readiness probe.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="IHealthCheck"/> to be added.</typeparam>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">Name for the check. This allows to distinguish it from other health checks.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    public static IHealthChecksBuilder AddReadinessCheck<T>(this IHealthChecksBuilder builder, string name)
        where T : class, IHealthCheck
    {
        return builder
            .AddCheck<T>(name, tags: _readinessTags);
    }
}
