// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Probes;

/// <summary>
/// Options for Kubernetes probes.
/// </summary>
public partial class KubernetesProbesOptions
{
    private const int DefaultLivenessProbePort = 2305;
    private const int DefaultStartupProbePort = 2306;
    private const int DefaultReadinessProbePort = 2307;

    /// <summary>
    /// Gets or sets the options for the liveness probe.
    /// </summary>
    /// <remarks>
    /// Default port is 2305.
    /// </remarks>
    public EndpointOptions LivenessProbe { get; set; } = new EndpointOptions
    {
        TcpPort = DefaultLivenessProbePort,
    };

    /// <summary>
    /// Gets or sets the options for the startup probe.
    /// </summary>
    /// <remarks>
    /// Default port is 2306.
    /// </remarks>
    public EndpointOptions StartupProbe { get; set; } = new EndpointOptions
    {
        TcpPort = DefaultStartupProbePort,
    };

    /// <summary>
    /// Gets or sets the options for the readiness probe.
    /// </summary>
    /// <remarks>
    /// Default port is 2307.
    /// </remarks>
    public EndpointOptions ReadinessProbe { get; set; } = new EndpointOptions
    {
        TcpPort = DefaultReadinessProbePort,
    };
}
