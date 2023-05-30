// Assembly 'Microsoft.Extensions.Telemetry'

namespace Microsoft.Extensions.Telemetry.Tracing;

public enum SamplerType
{
    AlwaysOn = 0,
    AlwaysOff = 1,
    TraceIdRatioBased = 2,
    ParentBased = 3
}
