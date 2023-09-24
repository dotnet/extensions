// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

namespace Microsoft.Extensions.Diagnostics.Latency;

public interface ILatencyContextTokenIssuer
{
    TagToken GetTagToken(string name);
    CheckpointToken GetCheckpointToken(string name);
    MeasureToken GetMeasureToken(string name);
}
