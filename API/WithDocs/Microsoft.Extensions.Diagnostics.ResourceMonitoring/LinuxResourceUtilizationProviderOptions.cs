// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Options for Linux resource utilization provider.
/// </summary>
public class LinuxResourceUtilizationProviderOptions
{
    /// <summary>
    /// Gets or sets the default interval used for refreshing values reported by <see cref="P:Microsoft.Extensions.Diagnostics.ResourceMonitoring.LinuxResourceUtilizationCounters.CpuConsumptionPercentage" />.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    /// <remarks>
    /// This is the time interval for a metric value to fetch resource utilization data from the operating system.
    /// </remarks>
    [TimeSpan(100, 900000)]
    public TimeSpan CpuConsumptionRefreshInterval { get; set; }

    /// <summary>
    /// Gets or sets the default interval used for refreshing values reported by <see cref="P:Microsoft.Extensions.Diagnostics.ResourceMonitoring.LinuxResourceUtilizationCounters.MemoryConsumptionPercentage" />.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    /// <remarks>
    /// This is the time interval for a metric value to fetch resource utilization data from the operating system.
    /// </remarks>
    [TimeSpan(100, 900000)]
    public TimeSpan MemoryConsumptionRefreshInterval { get; set; }

    public LinuxResourceUtilizationProviderOptions();
}
