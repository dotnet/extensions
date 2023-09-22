// Assembly 'Microsoft.Extensions.Diagnostics.HealthChecks.ResourceUtilization'

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

public class ResourceUtilizationHealthCheckOptions
{
    [ValidateObjectMembers]
    public ResourceUsageThresholds CpuThresholds { get; set; }
    [ValidateObjectMembers]
    public ResourceUsageThresholds MemoryThresholds { get; set; }
    [Microsoft.Shared.Data.Validation.TimeSpan(100, int.MaxValue)]
    public TimeSpan SamplingWindow { get; set; }
    public ResourceUtilizationHealthCheckOptions();
}
