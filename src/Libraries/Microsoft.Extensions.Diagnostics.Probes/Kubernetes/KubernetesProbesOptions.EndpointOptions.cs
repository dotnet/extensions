// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.Probes;

public partial class KubernetesProbesOptions
{
    /// <summary>
    /// Options to control TCP-based health check probes.
    /// </summary>
    [SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used", Justification = "In place numbers make the ranges cleaner")]
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "It's fine")]
    public class EndpointOptions
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
        /// Gets or sets a predicate that is used to include health checks based on user-defined criteria.
        /// </summary>
        /// <remarks>
        /// Default set to <see langword="null" /> which has the effect of enabling all health checks.
        /// </remarks>
        public Func<HealthCheckRegistration, bool>? FilterChecks { get; set; }
    }
}
