// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Options for <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.IResourceMonitor" />.
/// </summary>
public class ResourceMonitoringOptions
{
    /// <summary>
    /// Gets or sets the maximum time window for which utilization can be requested.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    [TimeSpan(100, 900000)]
    public TimeSpan CollectionWindow { get; set; }

    /// <summary>
    /// Gets or sets the interval at which a new utilization sample is captured.
    /// </summary>
    /// <value>
    /// The default value is 1 second.
    /// </value>
    [TimeSpan(1, 900000)]
    public TimeSpan SamplingInterval { get; set; }

    /// <summary>
    /// Gets or sets the default period used for utilization calculation.
    /// </summary>
    /// <value>
    /// The default value is 5 seconds.
    /// </value>
    /// <remarks>
    /// The value needs to be less than or equal to the <see cref="P:Microsoft.Extensions.Diagnostics.ResourceMonitoring.ResourceMonitoringOptions.CollectionWindow" />.
    /// Most importantly, this period is used to calculate <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.Utilization" /> instances pushed to publishers.
    /// </remarks>
    [Experimental("EXTEXP0008", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [TimeSpan(100, 900000)]
    public TimeSpan CalculationPeriod { get; set; }

    public ResourceMonitoringOptions();
}
