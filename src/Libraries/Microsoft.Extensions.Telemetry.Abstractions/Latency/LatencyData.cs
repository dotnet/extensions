// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Diagnostics.Latency;

/// <summary>
/// Encapsulates the state accumulated while measuring the latency of an operation.
/// </summary>
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Comparing instances is not an expected scenario")]
public readonly struct LatencyData
{
    private readonly ArraySegment<Tag> _tags;
    private readonly ArraySegment<Checkpoint> _checkpoints;
    private readonly ArraySegment<Measure> _measures;

    /// <summary>
    /// Initializes a new instance of the <see cref="LatencyData"/> struct.
    /// </summary>
    /// <param name="tags">List of tags.</param>
    /// <param name="checkpoints">List of checkpoints.</param>
    /// <param name="measures">List of measures.</param>
    /// <param name="durationTimestamp">Total duration of the operation that is represented by this data.</param>
    /// <param name="durationTimestampFrequency">Frequency of the duration timestamp.</param>
    public LatencyData(ArraySegment<Tag> tags, ArraySegment<Checkpoint> checkpoints, ArraySegment<Measure> measures, long durationTimestamp, long durationTimestampFrequency)
    {
        _tags = tags;
        _checkpoints = checkpoints;
        _measures = measures;
        DurationTimestamp = durationTimestamp;
        DurationTimestampFrequency = durationTimestampFrequency;
    }

    /// <summary>
    /// Gets the list of checkpoints added while measuring the operation's latency.
    /// </summary>
    public ReadOnlySpan<Checkpoint> Checkpoints => _checkpoints;

    /// <summary>
    /// Gets the list of tags added to provide metadata about the operation being measured.
    /// </summary>
    public ReadOnlySpan<Tag> Tags => _tags;

    /// <summary>
    /// Gets the list of measures added.
    /// </summary>
    public ReadOnlySpan<Measure> Measures => _measures;

    /// <summary>
    /// Gets the total time measured by the latency context.
    /// </summary>
    public long DurationTimestamp { get; }

    /// <summary>
    /// Gets the frequency of the duration timestamp.
    /// </summary>
    public long DurationTimestampFrequency { get; }
}
