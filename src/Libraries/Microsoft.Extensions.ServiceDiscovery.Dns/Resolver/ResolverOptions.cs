// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

/// <summary>
/// Provides configuration options for DNS resolution, including server endpoints, retry attempts, and timeout settings.
/// </summary>
public class ResolverOptions
{
    /// <summary>
    /// Gets or sets the collection of server endpoints used for network connections.
    /// </summary>
    public IList<IPEndPoint> Servers { get; set; } = new List<IPEndPoint>();

    /// <summary>
    /// Gets or sets the number of allowed attempts for the operation.
    /// </summary>
    public int Attempts { get; set; } = 2;

    /// <summary>
    /// Gets or sets the maximum duration to wait for an operation to complete before timing out.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);

    // override for testing purposes
    internal Func<Memory<byte>, int, int>? _transportOverride;
}
