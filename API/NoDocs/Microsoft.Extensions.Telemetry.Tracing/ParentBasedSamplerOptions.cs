// Assembly 'Microsoft.Extensions.Telemetry'

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Telemetry.Tracing;

public class ParentBasedSamplerOptions
{
    public SamplerType RootSamplerType { get; set; }
    [ValidateObjectMembers]
    public TraceIdRatioBasedSamplerOptions? TraceIdRatioBasedSamplerOptions { get; set; }
    public ParentBasedSamplerOptions();
}
