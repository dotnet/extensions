// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Probes;

/// <summary>
/// Extensions for setting up probes for Kubernetes.
/// </summary>
public static class KubernetesProbesExtensions
{
    /// <summary>
    /// Registers liveness, startup and readiness probes using the default options.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHealthChecksBuilder AddKubernetesProbes(this IHealthChecksBuilder builder)
    {
        return builder.AddKubernetesProbes((_) => { });
    }

    /// <summary>
    /// Registers liveness, startup and readiness probes using the configured options.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="section">Configuration for <see cref="KubernetesProbesOptions"/>.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHealthChecksBuilder AddKubernetesProbes(this IHealthChecksBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        return builder.AddKubernetesProbes(section.Bind);
    }

    /// <summary>
    /// Registers liveness, startup and readiness probes using the configured options.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="configure">Configure action for <see cref="KubernetesProbesOptions"/>.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHealthChecksBuilder AddKubernetesProbes(this IHealthChecksBuilder builder, Action<KubernetesProbesOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        var wrapperOptions = new KubernetesProbesOptions();

        _ = builder.Services
            .AddTcpEndpointHealthCheck(ProbeTags.Liveness, (options) =>
            {
                wrapperOptions.LivenessProbe = options;
                configure(wrapperOptions);
                var originalPredicate = options.PublishingPredicate;
                options.PublishingPredicate = (check) => check.Tags.Contains(ProbeTags.Liveness) && originalPredicate(check);
            })
            .AddTcpEndpointHealthCheck(ProbeTags.Startup, (options) =>
            {
                wrapperOptions.StartupProbe = options;
                configure(wrapperOptions);
                var originalPredicate = options.PublishingPredicate;
                options.PublishingPredicate = (check) => check.Tags.Contains(ProbeTags.Startup) && originalPredicate(check);
            })
            .AddTcpEndpointHealthCheck(ProbeTags.Readiness, (options) =>
            {
                wrapperOptions.ReadinessProbe = options;
                configure(wrapperOptions);
                var originalPredicate = options.PublishingPredicate;
                options.PublishingPredicate = (check) => check.Tags.Contains(ProbeTags.Readiness) && originalPredicate(check);
            });

        return builder;
    }
}
