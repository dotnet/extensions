// Assembly 'Microsoft.Extensions.Diagnostics.Probes'

using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.Probes;

/// <summary>
/// Options for Kubernetes probes.
/// </summary>
public class KubernetesProbesOptions
{
    /// <summary>
    /// Options to control TCP-based health check probes.
    /// </summary>
    public class EndpointOptions
    {
        /// <summary>
        /// Gets or sets the TCP port that gets opened if the service is healthy and closed otherwise.
        /// </summary>
        /// <value>
        /// The default value is 2305.
        /// </value>
        [Range(1, 65535)]
        public int TcpPort { get; set; }

        /// <summary>
        /// Gets or sets the maximum length of the pending connections queue.
        /// </summary>
        /// <value>
        /// The default value is 10.
        /// </value>
        [Range(1, 10000)]
        public int MaxPendingConnections { get; set; }

        /// <summary>
        /// Gets or sets the interval at which the health of the application is assessed.
        /// </summary>
        /// <value>
        /// The default value is 30 seconds.
        /// </value>
        [TimeSpan("00:00:05", "00:05:00")]
        public TimeSpan Period { get; set; }

        /// <summary>
        /// Gets or sets a predicate that is used to include health checks based on user-defined criteria.
        /// </summary>
        /// <value>
        /// The default value is <see langword="null" />, which has the effect of enabling all health checks.
        /// </value>
        public Func<HealthCheckRegistration, bool>? FilterChecks { get; set; }

        public EndpointOptions();
    }

    /// <summary>
    /// Gets or sets the options for the liveness probe.
    /// </summary>
    /// <remarks>
    /// Default port is 2305.
    /// </remarks>
    public EndpointOptions LivenessProbe { get; set; }

    /// <summary>
    /// Gets or sets the options for the startup probe.
    /// </summary>
    /// <remarks>
    /// Default port is 2306.
    /// </remarks>
    public EndpointOptions StartupProbe { get; set; }

    /// <summary>
    /// Gets or sets the options for the readiness probe.
    /// </summary>
    /// <remarks>
    /// Default port is 2307.
    /// </remarks>
    public EndpointOptions ReadinessProbe { get; set; }

    public KubernetesProbesOptions();
}
