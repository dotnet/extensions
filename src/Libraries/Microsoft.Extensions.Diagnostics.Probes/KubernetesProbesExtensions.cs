﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Probes;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for setting up probes for Kubernetes.
/// </summary>
public static class KubernetesProbesExtensions
{
    /// <summary>
    /// Registers liveness, startup and readiness probes using the default options.
    /// </summary>
    /// <param name="services">The dependency injection container to add the probe to.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddKubernetesProbes(this IServiceCollection services)
        => services.AddKubernetesProbes((_) => { });

    /// <summary>
    /// Registers liveness, startup and readiness probes using the configured options.
    /// </summary>
    /// <param name="services">The dependency injection container to add the probe to.</param>
    /// <param name="section">Configuration for <see cref="KubernetesProbesOptions"/>.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddKubernetesProbes(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        return services.AddKubernetesProbes(o => section.Bind(o));
    }

    /// <summary>
    /// Registers liveness, startup and readiness probes using the configured options.
    /// </summary>
    /// <param name="services">The dependency injection container to add the probe to.</param>
    /// <param name="configure">Configure action for <see cref="KubernetesProbesOptions"/>.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddKubernetesProbes(this IServiceCollection services, Action<KubernetesProbesOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        _ = services
            .AddOptionsWithValidateOnStart<KubernetesProbesOptions, KubernetesProbesOptionsValidator>()
            .Configure(configure);

        var wrapperOptions = new KubernetesProbesOptions();

        return services
            .AddTcpEndpointProbe(ProbeTags.Liveness, options =>
            {
                options.TcpPort = wrapperOptions.LivenessProbe.TcpPort;
                wrapperOptions.LivenessProbe = options;

                configure(wrapperOptions);

                var originalPredicate = options.FilterChecks;
                if (originalPredicate == null)
                {
                    options.FilterChecks = (check) => check.Tags.Contains(ProbeTags.Liveness);
                }
                else
                {
                    options.FilterChecks = (check) => check.Tags.Contains(ProbeTags.Liveness) && originalPredicate(check);
                }
            })
            .AddTcpEndpointProbe(ProbeTags.Startup, options =>
            {
                options.TcpPort = wrapperOptions.StartupProbe.TcpPort;
                wrapperOptions.StartupProbe = options;

                configure(wrapperOptions);

                var originalPredicate = options.FilterChecks;
                if (originalPredicate == null)
                {
                    options.FilterChecks = (check) => check.Tags.Contains(ProbeTags.Startup);
                }
                else
                {
                    options.FilterChecks = (check) => check.Tags.Contains(ProbeTags.Startup) && originalPredicate(check);
                }
            })
            .AddTcpEndpointProbe(ProbeTags.Readiness, (options) =>
            {
                options.TcpPort = wrapperOptions.ReadinessProbe.TcpPort;
                wrapperOptions.ReadinessProbe = options;

                configure(wrapperOptions);

                var originalPredicate = options.FilterChecks;
                if (originalPredicate == null)
                {
                    options.FilterChecks = (check) => check.Tags.Contains(ProbeTags.Readiness);
                }
                else
                {
                    options.FilterChecks = (check) => check.Tags.Contains(ProbeTags.Readiness) && originalPredicate(check);
                }
            });
    }
}
