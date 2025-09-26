// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 
/// </summary>
public static class KubernetesResourceQuotasServiceCollectionsExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configureOptions"></param>
    /// <returns></returns>
    public static IServiceCollection AddKubernetesResourceQuotas(
        this IServiceCollection services,
        Action<KubernetesMetadata>? configureOptions = null)
    {
        // Simple - just register before AddResourceMonitoring() is called
        services.TryAddSingleton<IResourceQuotasProvider, KubernetesResourceQuotasProvider>();

        if (configureOptions != null)
        {
            _ = services.Configure(configureOptions);
        }

        services.AddResourceMonitoring();

        return services;
    }
}
