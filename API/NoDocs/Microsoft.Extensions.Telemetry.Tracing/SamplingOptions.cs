// Assembly 'Microsoft.Extensions.Telemetry'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Tracing;

public class SamplingOptions
{
    public bool ParentBased { get; set; }
    [EnumDataType(typeof(SamplerType))]
    public SamplerType SamplerType { get; set; }
    [Range(0.0, 1.0)]
    public double SampleRate { get; set; }
    public SamplingOptions();
}
