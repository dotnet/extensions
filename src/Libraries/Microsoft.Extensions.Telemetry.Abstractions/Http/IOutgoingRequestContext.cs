// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Telemetry;

/// <summary>
/// Interface that holds outgoing request metadata.
/// </summary>
public interface IOutgoingRequestContext
{
    /// <summary>
    /// Gets or sets the metadata for outgoing requests.
    /// </summary>
    RequestMetadata? RequestMetadata { get; set; }
}
