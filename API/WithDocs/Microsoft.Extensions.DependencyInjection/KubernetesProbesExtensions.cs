// Assembly 'Microsoft.Extensions.Diagnostics.Probes'

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.Probes;

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
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddKubernetesProbes(this IServiceCollection services);

    /// <summary>
    /// Registers liveness, startup and readiness probes using the configured options.
    /// </summary>
    /// <param name="services">The dependency injection container to add the probe to.</param>
    /// <param name="section">Configuration for <see cref="T:Microsoft.Extensions.Diagnostics.Probes.KubernetesProbesOptions" />.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddKubernetesProbes(this IServiceCollection services, IConfigurationSection section);

    /// <summary>
    /// Registers liveness, startup and readiness probes using the configured options.
    /// </summary>
    /// <param name="services">The dependency injection container to add the probe to.</param>
    /// <param name="configure">Configure action for <see cref="T:Microsoft.Extensions.Diagnostics.Probes.KubernetesProbesOptions" />.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddKubernetesProbes(this IServiceCollection services, Action<KubernetesProbesOptions> configure);
}
