// Assembly 'Microsoft.Extensions.Diagnostics.HealthChecks.ResourceUtilization'

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Options for the resource utilization health check.
/// </summary>
public class ResourceUtilizationHealthCheckOptions
{
    /// <summary>
    /// Gets or sets thresholds for CPU utilization.
    /// </summary>
    /// <remarks>
    /// The thresholds are periodically compared against the utilization samples provided by
    /// the registered <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.IResourceMonitor" />.
    /// </remarks>
    [ValidateObjectMembers]
    public ResourceUsageThresholds CpuThresholds { get; set; }

    /// <summary>
    /// Gets or sets thresholds for memory utilization.
    /// </summary>
    /// <remarks>
    /// The thresholds are periodically compared against the utilization samples provided by
    /// the registered <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.IResourceMonitor" />.
    /// </remarks>
    [ValidateObjectMembers]
    public ResourceUsageThresholds MemoryThresholds { get; set; }

    /// <summary>
    /// Gets or sets the time window for used for calculating CPU and memory utilization averages.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    [Microsoft.Shared.Data.Validation.TimeSpan(100, int.MaxValue)]
    public TimeSpan SamplingWindow { get; set; }

    public ResourceUtilizationHealthCheckOptions();
}
