// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

/// <summary>
/// Provides configuration options for DNS resolution, including server endpoints, retry attempts, and timeout settings.
/// </summary>
public class DnsResolverOptions
{
    /// <summary>
    /// Gets or sets the collection of server endpoints used for network connections.
    /// </summary>
    public IList<IPEndPoint> Servers { get; set; } = new List<IPEndPoint>();

    /// <summary>
    /// Gets or sets the maximum number of attempts per server.
    /// </summary>
    public int MaxAttempts { get; set; } = 2;

    /// <summary>
    /// Gets or sets the maximum duration per attempt to wait before timing out.
    /// </summary>
    /// <remarks>
    /// The maximum time for resolving a query is <see cref="MaxAttempts"/> * <see cref="Servers"/> count * <see cref="Timeout"/>.
    /// </remarks>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);

    // override for testing purposes
    internal Func<Memory<byte>, int, int>? _transportOverride;
}
