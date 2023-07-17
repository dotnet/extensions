// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Options for the telemetry health check publisher.
/// </summary>
[Experimental(diagnosticId: Experiments.HealthChecks, UrlFormat = Experiments.UrlFormat)]
public class TelemetryHealthCheckPublisherOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to log only when unhealthy reports are received.
    /// </summary>
    /// <value>
    /// <see langword="true"/> to only log unhealthy reports; <see langword="false"/> to always log.
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool LogOnlyUnhealthy { get; set; }
}
