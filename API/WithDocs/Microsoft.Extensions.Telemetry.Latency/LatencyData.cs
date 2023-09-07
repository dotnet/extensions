// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Encapsulates the state accumulated while measuring the latency of an operaiton.
/// </summary>
public readonly struct LatencyData
{
    /// <summary>
    /// Gets the list of checkpoints added while measuring the operation's latency.
    /// </summary>
    public ReadOnlySpan<Checkpoint> Checkpoints { get; }

    /// <summary>
    /// Gets the list of tags added to provide metadata about the operation being measured.
    /// </summary>
    public ReadOnlySpan<Tag> Tags { get; }

    /// <summary>
    /// Gets the list of measures added.
    /// </summary>
    public ReadOnlySpan<Measure> Measures { get; }

    /// <summary>
    /// Gets the total time measured by the latency context.
    /// </summary>
    public long DurationTimestamp { get; }

    /// <summary>
    /// Gets the frequency of the duration timestamp.
    /// </summary>
    public long DurationTimestampFrequency { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Latency.LatencyData" /> struct.
    /// </summary>
    /// <param name="tags">List of tags.</param>
    /// <param name="checkpoints">List of checkpoints.</param>
    /// <param name="measures">List of measures.</param>
    /// <param name="durationTimestamp">Total duration of the operation that is represented by this data.</param>
    /// <param name="durationTimestampFrequency">Frequency of the duration timestamp.</param>
    public LatencyData(ArraySegment<Tag> tags, ArraySegment<Checkpoint> checkpoints, ArraySegment<Measure> measures, long durationTimestamp, long durationTimestampFrequency);
}
