// Assembly 'Microsoft.Extensions.Telemetry'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Tracing;

public class TraceIdRatioBasedSamplerOptions
{
    [Range(0.0, 1.0)]
    public double Probability { get; set; }
    public TraceIdRatioBasedSamplerOptions();
}
