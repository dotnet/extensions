// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Options to control resource monitoring behavior.
/// </summary>
public class ResourceMonitoringOptions
{
    /// <summary>
    /// Gets or sets the maximum time window for which utilization can be requested.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    /// <remarks>
    /// This value represents the total amount of time for which the resource monitor tracks utilization
    /// information for the system.
    /// </remarks>
    [TimeSpan(100, 900000)]
    public TimeSpan CollectionWindow { get; set; }

    /// <summary>
    /// Gets or sets the interval at which a new utilization sample is captured.
    /// </summary>
    /// <value>
    /// The default value is 1 second.
    /// </value>
    /// <remarks>
    /// This value must be &lt;= to <see cref="P:Microsoft.Extensions.Diagnostics.ResourceMonitoring.ResourceMonitoringOptions.CollectionWindow" />.
    /// </remarks>
    [TimeSpan(1, 900000)]
    public TimeSpan SamplingInterval { get; set; }

    /// <summary>
    /// Gets or sets the observation window used to calculate the <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.ResourceUtilization" /> instances pushed to publishers.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    /// <remarks>
    /// The value needs to be &lt;= to <see cref="P:Microsoft.Extensions.Diagnostics.ResourceMonitoring.ResourceMonitoringOptions.CollectionWindow" />.
    /// </remarks>
    [TimeSpan(100, 900000)]
    public TimeSpan PublishingWindow { get; set; }

    /// <summary>
    /// Gets or sets the default interval used for refreshing values reported by <see cref="P:Microsoft.Extensions.Diagnostics.ResourceMonitoring.ResourceUtilizationCounters.CpuConsumptionPercentage" />.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    /// <remarks>
    /// This property is Linux-specific and has no effect on other operating systems.
    /// This is the time interval for a metric value to fetch resource utilization data from the operating system.
    /// </remarks>
    [TimeSpan(100, 900000)]
    public TimeSpan CpuConsumptionRefreshInterval { get; set; }

    /// <summary>
    /// Gets or sets the default interval used for refreshing values reported by <see cref="P:Microsoft.Extensions.Diagnostics.ResourceMonitoring.ResourceUtilizationCounters.MemoryConsumptionPercentage" />.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    /// <remarks>
    /// This property is Linux-specific and has no effect on other operating systems.
    /// This is the time interval for a metric value to fetch resource utilization data from the operating system.
    /// </remarks>
    [TimeSpan(100, 900000)]
    public TimeSpan MemoryConsumptionRefreshInterval { get; set; }

    /// <summary>
    /// Gets or sets the list of source IPv4 addresses to track the connections for in telemetry.
    /// </summary>
    /// <remarks>
    /// This property is Windows-specific and has no effect on other operating systems.
    /// </remarks>
    [Required]
    public ISet<string> SourceIpAddresses { get; set; }

    public ResourceMonitoringOptions();
}
