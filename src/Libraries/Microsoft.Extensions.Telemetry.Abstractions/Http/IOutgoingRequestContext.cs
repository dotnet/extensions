// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Telemetry;

/// <summary>
/// Interface to represent outgoing request context.
/// </summary>
public interface IOutgoingRequestContext
{
    /// <summary>
    /// Sets metadata for outgoing requests to be used for telemetry purposes.
    /// </summary>
    /// <param name="metadata">Metadata for the request.</param>
    void SetRequestMetadata(RequestMetadata metadata);

    /// <summary>
    /// Gets metadata for outgoing requests to be used for telemetry purposes.
    /// </summary>
    /// <returns>request metadata.</returns>
    RequestMetadata? RequestMetadata { get; }
}
