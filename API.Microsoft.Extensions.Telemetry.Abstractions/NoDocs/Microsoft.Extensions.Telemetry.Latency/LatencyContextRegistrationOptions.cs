// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Latency;

public class LatencyContextRegistrationOptions
{
    [Required]
    public IReadOnlyList<string> CheckpointNames { get; set; }
    [Required]
    public IReadOnlyList<string> MeasureNames { get; set; }
    [Required]
    public IReadOnlyList<string> TagNames { get; set; }
    public LatencyContextRegistrationOptions();
}
