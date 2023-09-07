// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Telemetry.Latency;

public interface ILatencyContextTokenIssuer
{
    TagToken GetTagToken(string name);
    CheckpointToken GetCheckpointToken(string name);
    MeasureToken GetMeasureToken(string name);
}
