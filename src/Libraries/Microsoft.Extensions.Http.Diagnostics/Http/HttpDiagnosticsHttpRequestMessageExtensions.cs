// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using System.Collections.Generic;
#endif
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace System.Net.Http;

/// <summary>
/// Extensions for telemetry utilities.
/// </summary>
public static class HttpDiagnosticsHttpRequestMessageExtensions
{
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
}
