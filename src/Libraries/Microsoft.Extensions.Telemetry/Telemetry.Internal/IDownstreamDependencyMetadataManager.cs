// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Http.Telemetry;

namespace Microsoft.Extensions.Telemetry.Internal;

/// <summary>
/// Interface to manage dependency metadata.
/// </summary>
internal interface IDownstreamDependencyMetadataManager
{
    /// <summary>
    /// Get metadata for the given request.
    /// </summary>
    /// <param name="requestMessage">Request object.</param>
    /// <returns><see cref="RequestMetadata"/> object.</returns>
    RequestMetadata? GetRequestMetadata(HttpRequestMessage requestMessage);

    /// <summary>
    /// Get metadata for the given request.
    /// </summary>
    /// <param name="requestMessage">Request object.</param>
    /// <returns><see cref="RequestMetadata"/> object.</returns>
    RequestMetadata? GetRequestMetadata(HttpWebRequest requestMessage);
}
