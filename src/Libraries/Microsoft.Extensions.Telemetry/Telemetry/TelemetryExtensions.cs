// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using System.Collections.Generic;
#endif
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry;

/// <summary>
/// Extensions for telemetry utilities.
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Sets metadata for outgoing requests to be used for telemetry purposes.
    /// </summary>
    /// <param name="request"><see cref="HttpWebRequest"/> object.</param>
    /// <param name="metadata">Metadata for the request.</param>
    public static void SetRequestMetadata(this HttpWebRequest request, RequestMetadata metadata)
    {
        _ = Throw.IfNull(request);
        _ = Throw.IfNull(metadata);

        request.Headers.Add(Constants.HttpWebConstants.RequestRouteHeader, metadata.RequestRoute);
        request.Headers.Add(Constants.HttpWebConstants.RequestNameHeader, metadata.RequestName);
        request.Headers.Add(Constants.HttpWebConstants.DependencyNameHeader, metadata.DependencyName);
    }

    /// <summary>
    /// Sets metadata for outgoing requests to be used for telemetry purposes.
    /// </summary>
    /// <param name="request"><see cref="HttpRequestMessage"/> object.</param>
    /// <param name="metadata">Metadata for the request.</param>
    public static void SetRequestMetadata(this HttpRequestMessage request, RequestMetadata metadata)
    {
        _ = Throw.IfNull(request);
        _ = Throw.IfNull(metadata);

#if NET5_0_OR_GREATER
        _ = request.Options.TryAdd(TelemetryConstants.RequestMetadataKey, metadata);
#else
        request.Properties.Add(TelemetryConstants.RequestMetadataKey, metadata);
#endif
    }

    /// <summary>
    /// Gets metadata for outgoing requests to be used for telemetry purposes.
    /// </summary>
    /// <param name="request"><see cref="HttpWebRequest"/> object.</param>
    /// <returns>Request metadata.</returns>
    public static RequestMetadata? GetRequestMetadata(this HttpWebRequest request)
    {
        _ = Throw.IfNull(request);

        string? requestRoute = request.Headers.Get(Constants.HttpWebConstants.RequestRouteHeader);

        if (requestRoute == null)
        {
            return null;
        }

        string? dependencyName = request.Headers.Get(Constants.HttpWebConstants.DependencyNameHeader);
        string? requestName = request.Headers.Get(Constants.HttpWebConstants.RequestNameHeader);

        var requestMetadata = new RequestMetadata
        {
            RequestRoute = requestRoute,
            RequestName = string.IsNullOrEmpty(requestName) ? TelemetryConstants.Unknown : requestName,
            DependencyName = string.IsNullOrEmpty(dependencyName) ? TelemetryConstants.Unknown : dependencyName
        };

        return requestMetadata;
    }

    /// <summary>
    /// Gets metadata for outgoing requests to be used for telemetry purposes.
    /// </summary>
    /// <param name="request"><see cref="HttpRequestMessage"/> object.</param>
    /// <returns>Request metadata or <see langword="null"/>.</returns>
    public static RequestMetadata? GetRequestMetadata(this HttpRequestMessage request)
    {
        _ = Throw.IfNull(request);

#if NET5_0_OR_GREATER
        _ = request.Options.TryGetValue(new HttpRequestOptionsKey<RequestMetadata>(TelemetryConstants.RequestMetadataKey), out var metadata);
        return metadata;
#else
        _ = request.Properties.TryGetValue(TelemetryConstants.RequestMetadataKey, out var metadata);
        return (RequestMetadata?)metadata;
#endif
    }

    /// <summary>
    /// Adds dependency metadata.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> object instance.</param>
    /// <param name="downstreamDependencyMetadata">DownstreamDependencyMetadata object to add.</param>
    /// <returns><see cref="IServiceCollection"/> object for chaining.</returns>
    public static IServiceCollection AddDownstreamDependencyMetadata(this IServiceCollection services, IDownstreamDependencyMetadata downstreamDependencyMetadata)
    {
        _ = Throw.IfNull(services);
        services.TryAddSingleton<IDownstreamDependencyMetadataManager, DownstreamDependencyMetadataManager>();
        _ = services.AddSingleton<IDownstreamDependencyMetadata>(downstreamDependencyMetadata);

        return services;
    }

    /// <summary>
    /// Adds dependency metadata.
    /// </summary>
    /// <typeparam name="T"><see cref="IDownstreamDependencyMetadata"/> instance to be registered.</typeparam>
    /// <param name="services"><see cref="IServiceCollection"/> object instance.</param>
    /// <returns><see cref="IServiceCollection"/> object for chaining.</returns>
    public static IServiceCollection AddDownstreamDependencyMetadata<T>(this IServiceCollection services)
        where T : class, IDownstreamDependencyMetadata
    {
        _ = Throw.IfNull(services);
        services.TryAddSingleton<IDownstreamDependencyMetadataManager, DownstreamDependencyMetadataManager>();
        _ = services.AddSingleton<IDownstreamDependencyMetadata, T>();

        return services;
    }
}
