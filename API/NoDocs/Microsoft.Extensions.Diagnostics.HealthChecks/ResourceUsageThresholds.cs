// Assembly 'Microsoft.Extensions.Diagnostics.HealthChecks.ResourceUtilization'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

public class ResourceUsageThresholds
{
    [Range(0.0, 100.0)]
    public double? DegradedUtilizationPercentage { get; set; }
    [Range(0.0, 100.0)]
    public double? UnhealthyUtilizationPercentage { get; set; }
    public ResourceUsageThresholds();
}
