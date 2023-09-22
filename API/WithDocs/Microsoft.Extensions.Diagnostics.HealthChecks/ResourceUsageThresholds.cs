// Assembly 'Microsoft.Extensions.Diagnostics.HealthChecks.ResourceUtilization'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Threshold settings for <see cref="T:Microsoft.Extensions.Diagnostics.HealthChecks.ResourceUtilizationHealthCheckOptions" />.
/// </summary>
public class ResourceUsageThresholds
{
    /// <summary>
    /// Gets or sets the percentage threshold for the degraded state.
    /// </summary>
    /// <value>
    /// The default value is <see langword="null" />.
    /// </value>
    [Range(0.0, 100.0)]
    public double? DegradedUtilizationPercentage { get; set; }

    /// <summary>
    /// Gets or sets the percentage threshold for the unhealthy state.
    /// </summary>
    /// <value>
    /// The default value is <see langword="null" />.
    /// </value>
    [Range(0.0, 100.0)]
    public double? UnhealthyUtilizationPercentage { get; set; }

    public ResourceUsageThresholds();
}
