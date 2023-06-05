// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.Probes;

/// <summary>
/// Options to control TCP-based health check probes.
/// </summary>
[SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used", Justification = "In place numbers make the ranges cleaner")]
public class TcpEndpointHealthCheckOptions
{
    private const int DefaultMaxPendingConnections = 10;
    private const int DefaultTcpPort = 2305;

    /// <summary>
    /// Gets or sets the TCP port which gets opened if service is healthy and closed otherwise.
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

    /// <summary>
    /// Gets or sets the interval at which the health of the application is assessed.
    /// </summary>
    /// <remarks>
    /// Default set to 30 seconds.
    /// </remarks>
    [TimeSpan("00:00:05", "00:05:00")]
    public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a predicate that can be used to include health checks based on user-defined criteria.
    /// </summary>
    /// <remarks>
    /// Default set to a predicate that accepts all health checks.
    /// </remarks>
    public Func<HealthCheckRegistration, bool> PublishingPredicate { get; set; } = (_) => true;
}
