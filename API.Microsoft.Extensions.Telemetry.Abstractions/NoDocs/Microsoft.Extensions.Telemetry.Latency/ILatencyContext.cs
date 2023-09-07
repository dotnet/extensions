// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;

namespace Microsoft.Extensions.Telemetry.Latency;

public interface ILatencyContext : IDisposable
{
    LatencyData LatencyData { get; }
    void SetTag(TagToken token, string value);
    void AddCheckpoint(CheckpointToken token);
    void AddMeasure(MeasureToken token, long value);
    void RecordMeasure(MeasureToken token, long value);
    void Freeze();
}
