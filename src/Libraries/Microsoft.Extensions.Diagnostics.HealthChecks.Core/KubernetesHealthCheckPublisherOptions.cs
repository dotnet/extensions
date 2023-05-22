// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Options to control the Kubernetes health status publisher.
/// </summary>
[SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used", Justification = "In place numbers make the ranges cleaner")]
public class KubernetesHealthCheckPublisherOptions
{
    private const int DefaultMaxPendingConnections = 10;
    private const int DefaultTcpPort = 2305;

    /// <summary>
    /// Gets or sets the TCP port which gets opened if the application is healthy and closed otherwise.
    /// </summary>
    /// <remarks>
    /// Default set to 2305.
    /// </remarks>
    [Range(1, 65535)]
    public int TcpPort { get; set; } = DefaultTcpPort;

    /// <summary>
    /// Gets or sets the maximum length of the pending connections queue.
    /// </summary>
    /// <remarks>
    /// Default set to 10.
    /// </remarks>
    [Range(1, 10000)]
    public int MaxPendingConnections { get; set; } = DefaultMaxPendingConnections;
}
