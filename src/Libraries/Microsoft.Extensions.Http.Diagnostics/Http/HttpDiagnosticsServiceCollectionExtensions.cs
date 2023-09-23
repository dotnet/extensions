// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Telemetry.Internal;
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
        services.TryAddSingleton<IDownstreamDependencyMetadataManager, DownstreamDependencyMetadataManager>();
        _ = services.AddSingleton(downstreamDependencyMetadata);

        return services;
    }

    /// <summary>
    /// Adds dependency metadata.
    /// </summary>
    /// <typeparam name="T"><see cref="IDownstreamDependencyMetadata"/> instance to be registered.</typeparam>
    /// <param name="services"><see cref="IServiceCollection"/> object instance.</param>
    /// <returns>The value of <paramref name="services"/>.</returns>
    public static IServiceCollection AddDownstreamDependencyMetadata<T>(this IServiceCollection services)
        where T : class, IDownstreamDependencyMetadata
    {
        _ = Throw.IfNull(services);
        services.TryAddSingleton<IDownstreamDependencyMetadataManager, DownstreamDependencyMetadataManager>();
        _ = services.AddSingleton<IDownstreamDependencyMetadata, T>();

        return services;
    }
}
