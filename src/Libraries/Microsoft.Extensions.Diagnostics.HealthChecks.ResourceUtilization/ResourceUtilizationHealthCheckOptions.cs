// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Options for the resource utilization health check.
/// </summary>
public class ResourceUtilizationHealthCheckOptions
{
    internal const int MinimumSamplingWindow = 100;
    internal static readonly TimeSpan DefaultSamplingWindow = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets thresholds for CPU utilization.
    /// </summary>
    /// <remarks>
    /// The thresholds are periodically compared against the utilization samples provided by
    /// the registered <see cref="IResourceMonitor"/>.
    /// </remarks>
    [ValidateObjectMembers]
    public ResourceUsageThresholds CpuThresholds { get; set; } = new ResourceUsageThresholds();

    /// <summary>
    /// Gets or sets thresholds for memory utilization.
    /// </summary>
    /// <remarks>
    /// The thresholds are periodically compared against the utilization samples provided by
    /// the registered <see cref="IResourceMonitor"/>.
    /// </remarks>
    [ValidateObjectMembers]
    public ResourceUsageThresholds MemoryThresholds { get; set; } = new ResourceUsageThresholds();

    /// <summary>
    /// Gets or sets the time window for used for calculating CPU and memory utilization averages.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    [TimeSpan(MinimumSamplingWindow, int.MaxValue)]
    public TimeSpan SamplingWindow { get; set; } = DefaultSamplingWindow;
}
