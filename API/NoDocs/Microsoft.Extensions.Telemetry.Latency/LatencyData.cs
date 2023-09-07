// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Latency;

public readonly struct LatencyData
{
    public ReadOnlySpan<Checkpoint> Checkpoints { get; }
    public ReadOnlySpan<Tag> Tags { get; }
    public ReadOnlySpan<Measure> Measures { get; }
    public long DurationTimestamp { get; }
    public long DurationTimestampFrequency { get; }
    public LatencyData(ArraySegment<Tag> tags, ArraySegment<Checkpoint> checkpoints, ArraySegment<Measure> measures, long durationTimestamp, long durationTimestampFrequency);
}
