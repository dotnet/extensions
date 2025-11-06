// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Kubernetes;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Lets you configure and register Kubernetes resource monitoring components.
/// </summary>
public static class KubernetesResourceQuotaServiceCollectionExtensions
{
    /// <summary>
    /// Configures and adds an Kubernetes resource monitoring components to a service collection alltoghter with necessary basic resource monitoring components.
    /// </summary>
    /// <param name="services">The dependency injection container to add the Kubernetes resource monitoring to.</param>
    /// <param name="environmentVariablePrefix">Optional value of prefix used to read environment variables in the container.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <remarks>
    /// <para>
    /// If you have configured your Kubernetes container with Downward API to add environment variable <c>MYCLUSTER_LIMITS_CPU</c> with CPU limits,
    /// then you should pass <c>MYCLUSTER_</c> to <paramref name="environmentVariablePrefix"/> parameter. Environment variables will be read during DI Container resolution.
    /// </para>
    /// <para>
    /// <strong>Important:</strong> Do not call <see cref="ResourceMonitoringServiceCollectionExtensions.AddResourceMonitoring(IServiceCollection)"/> 
    /// if you are using this method, as it already includes all necessary resource monitoring components and registers a Kubernetes-specific 
    /// <see cref="ResourceQuotaProvider"/> implementation. Calling both methods may result in conflicting service registrations.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddKubernetesResourceMonitoring(
        this IServiceCollection services,
        string? environmentVariablePrefix = default)
    {
        services.TryAddSingleton<KubernetesMetadata>(serviceProvider =>
        {
            var metadata = new KubernetesMetadata(environmentVariablePrefix ?? string.Empty);
            return metadata.Build();
        });
        services.TryAddSingleton<ResourceQuotaProvider, KubernetesResourceQuotaProvider>();

        _ = services.AddResourceMonitoring();

        return services;
    }
}
