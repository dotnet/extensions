// Assembly 'Microsoft.Extensions.Telemetry'

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Telemetry.Tracing;

public class SamplingOptions
{
    public SamplerType SamplerType { get; set; }
    [ValidateObjectMembers]
    public ParentBasedSamplerOptions? ParentBasedSamplerOptions { get; set; }
    [ValidateObjectMembers]
    public TraceIdRatioBasedSamplerOptions? TraceIdRatioBasedSamplerOptions { get; set; }
    public SamplingOptions();
}
