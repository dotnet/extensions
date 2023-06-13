// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Abstraction that provides the context for latency measurement and diagnostics.
/// </summary>
/// <remarks>
/// The context ties in latency signals such as checkpoints and measures for a scope along with
/// mechanisms such as tags that allow describing the scope. For example, a context lets you record
/// tags, checkpoints and measures within the scope of a web request.
/// </remarks>
public interface ILatencyContext : IDisposable
{
    /// <summary>
    /// Adds a tag to the context.
    /// </summary>
    /// <param name="token">Tag token.</param>
    /// <param name="value">Value of the tag.</param>
    /// <remarks>
    /// Tags are used to provide metadata to the context. These are pivots that are useful to
    /// slice and dice the data for analysis. Examples include API, Client, UserType etc.
    /// Setting a tag with same name overrides its prior value i.e., last call wins.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    void SetTag(TagToken token, string value);

    /// <summary>
    /// Adds a checkpoint to the context.
    /// </summary>
    /// <param name="token">Checkpoint token.</param>
    /// <remarks>
    /// A checkpoint can be added only once per context. Use checkpoints for
    /// code that is non-reentrant per context.
    /// </remarks>
    void AddCheckpoint(CheckpointToken token);

    /// <summary>
    /// Adds to a measure.
    /// </summary>
    /// <param name="token">Measure token.</param>
    /// <param name="value">Value to add.</param>
    /// <remarks>
    /// Adds the value to a measure. Measures are used for tracking total latency
    /// or the count for repeating operations. Example: Latency for all database calls,
    /// number of calls to an external service, etc.</remarks>
    void AddMeasure(MeasureToken token, long value);

    /// <summary>
    /// Sets a measure to an absolute value.
    /// </summary>
    /// <param name="token">Measure token.</param>
    /// <param name="value">Value to set.</param>
    void RecordMeasure(MeasureToken token, long value);

    /// <summary>
    /// Stops the latency measurement.
    /// </summary>
    /// <remarks>This prevents any state change to the context.</remarks>
    void Freeze();

    /// <summary>
    /// Gets the accumulated latency data.
    /// </summary>
    LatencyData LatencyData { get; }
}
