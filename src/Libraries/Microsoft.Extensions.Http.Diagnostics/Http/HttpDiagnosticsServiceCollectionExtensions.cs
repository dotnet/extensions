// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for telemetry utilities.
/// </summary>
public static class HttpDiagnosticsServiceCollectionExtensions
{
    /// <summary>
    /// Adds dependency metadata.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> object instance.</param>
    /// <param name="downstreamDependencyMetadata">DownstreamDependencyMetadata object to add.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddDownstreamDependencyMetadata(this IServiceCollection services, IDownstreamDependencyMetadata downstreamDependencyMetadata)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(downstreamDependencyMetadata);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDownstreamDependencyMetadata>(downstreamDependencyMetadata));
        services.TryAddSingleton<HttpDependencyMetadataResolver, DefaultHttpDependencyMetadataResolver>();
        return services;
    }

    /// <summary>
    /// Adds dependency metadata.
    /// </summary>
    /// <typeparam name="T"><see cref="IDownstreamDependencyMetadata"/> instance to be registered.</typeparam>
    /// <param name="services"><see cref="IServiceCollection"/> object instance.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddDownstreamDependencyMetadata<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services)
        where T : class, IDownstreamDependencyMetadata
    {
        _ = Throw.IfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDownstreamDependencyMetadata, T>());
        services.TryAddSingleton<HttpDependencyMetadataResolver, DefaultHttpDependencyMetadataResolver>();
        return services;
    }

    /// <summary>
    /// Adds services required for HTTP dependency metadata resolution.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddHttpDependencyMetadataResolver(this IServiceCollection services)
    {
        services.TryAddSingleton<HttpDependencyMetadataResolver, DefaultHttpDependencyMetadataResolver>();
        return services;
    }

    /// <summary>
    /// Adds services required for HTTP dependency metadata resolution with the specified metadata providers.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="providers">The HTTP dependency metadata providers to register.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddHttpDependencyMetadataResolver(
        this IServiceCollection services,
        params IDownstreamDependencyMetadata[] providers)
    {
        _ = Throw.IfNull(services);

        foreach (var provider in providers)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDownstreamDependencyMetadata>(provider));
        }

        services.TryAddSingleton<HttpDependencyMetadataResolver, DefaultHttpDependencyMetadataResolver>();
        return services;
    }
}
