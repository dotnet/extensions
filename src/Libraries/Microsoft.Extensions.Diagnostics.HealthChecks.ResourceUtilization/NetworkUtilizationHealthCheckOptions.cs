// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Options for the network utilization health check.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.HealthChecks, UrlFormat = DiagnosticIds.UrlFormat)]
public class NetworkUtilizationHealthCheckOptions
{
    /// <summary>
    /// Gets or sets thresholds for the number of established TCP connections.
    /// </summary>
    /// <remarks>
    /// The thresholds are periodically compared against the connection counts provided by the Resource Monitoring library.
    /// A large number of established connections can indicate resource exhaustion.
    /// </remarks>
    [ValidateObjectMembers]
    public ResourceCountThresholds EstablishedConnectionsThresholds { get; set; } = new ResourceCountThresholds();

    /// <summary>
    /// Gets or sets thresholds for the number of TCP connections in the SYN received state.
    /// </summary>
    /// <remarks>
    /// The thresholds are periodically compared against the connection counts provided by the Resource Monitoring library.
    /// A large number of connections stuck in the SYN received state can indicate a SYN flood or slow handshakes.
    /// </remarks>
    [ValidateObjectMembers]
    public ResourceCountThresholds SynReceivedConnectionsThresholds { get; set; } = new ResourceCountThresholds();
}
