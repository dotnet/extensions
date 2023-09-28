// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace System.Net;

/// <summary>
/// Extensions for telemetry utilities.
/// </summary>
public static class HttpDiagnosticsHttpWebRequestExtensions
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
}
