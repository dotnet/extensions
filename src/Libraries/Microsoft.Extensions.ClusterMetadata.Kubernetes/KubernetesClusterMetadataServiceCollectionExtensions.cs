// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ClusterMetadata.Kubernetes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <see cref="ServiceCollection"/> extensions for Kubernetes metadata.
/// </summary>
public static class KubernetesClusterMetadataServiceCollectionExtensions
{
    /// <summary>
    /// Adds an instance of <see cref="KubernetesClusterMetadata"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="section">The configuration section to bind.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddKubernetesClusterMetadata(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        _ = services.AddOptionsWithValidateOnStart<KubernetesClusterMetadata, KubernetesClusterMetadataValidator>().Bind(section);

        return services;
    }

    /// <summary>
    /// Adds an instance of <see cref="KubernetesClusterMetadata"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">The delegate to configure <see cref="KubernetesClusterMetadata"/> with.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddKubernetesClusterMetadata(this IServiceCollection services, Action<KubernetesClusterMetadata> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        _ = services.AddOptionsWithValidateOnStart<KubernetesClusterMetadata, KubernetesClusterMetadataValidator>().Configure(configure);

        return services;
    }
}
