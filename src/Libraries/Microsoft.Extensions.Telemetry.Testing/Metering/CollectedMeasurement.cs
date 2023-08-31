// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Testing.Metering;

/// <summary>
/// Represents a single measurement performed by an instrument.
/// </summary>
/// <typeparam name="T">The type of metric measurement value.</typeparam>
[DebuggerDisplay("{DebuggerToString(),nq}")]
public sealed class CollectedMeasurement<T>
    where T : struct
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectedMeasurement{T}"/> class.
    /// </summary>
    /// <param name="value">The measurement's value.</param>
    /// <param name="tags">The dimensions of this measurement.</param>
    /// <param name="timestamp">The time that the measurement occurred at.</param>
    internal CollectedMeasurement(T value, ReadOnlySpan<KeyValuePair<string, object?>> tags, DateTimeOffset timestamp)
    {
        var d = new Dictionary<string, object?>();
        foreach (var tag in tags)
        {
            d[tag.Key] = tag.Value;
        }

        Tags = d;
        Timestamp = timestamp;
        Value = value;
    }

    /// <summary>
    /// Gets a measurement's value.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Gets a timestamp indicating when the measurement was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the measurement's dimensions.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Tags { get; }

    /// <summary>
    /// Checks that the measurement includes a specific set of tags with specific values.
    /// </summary>
    /// <param name="tags">The set of tags to check.</param>
    /// <returns><see langword="true"/> if all the tags exist in the measurement with matching values, otherwise <see langword="false"/>.</returns>
    public bool ContainsTags(params KeyValuePair<string, object?>[] tags)
    {
        foreach (var kvp in Throw.IfNull(tags))
        {
            if (!Tags.TryGetValue(kvp.Key, out var value))
            {
                return false;
            }

            if (!object.Equals(kvp.Value, value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks that the measurement includes a specific set of tags with any value.
    /// </summary>
    /// <param name="tags">The set of tag names to check.</param>
    /// <returns><see langword="true"/> if all the tags exist in the measurement, otherwise <see langword="false"/>.</returns>
    public bool ContainsTags(params string[] tags)
    {
        foreach (var key in Throw.IfNull(tags))
        {
            if (!Tags.ContainsKey(key))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks that the measurement has an exactly matching set of tags with specific values.
    /// </summary>
    /// <param name="tags">The set of tags to check.</param>
    /// <returns><see langword="true"/> if all the tags exist in the measurement with matching values, otherwise <see langword="false"/>.</returns>
    public bool MatchesTags(params KeyValuePair<string, object?>[] tags) => ContainsTags(tags) && (Tags.Count == tags.Length);

    /// <summary>
    /// Checks that the measurement has a exactly matching set of tags with any value.
    /// </summary>
    /// <param name="tags">The set of tag names to check.</param>
    /// <returns><see langword="true"/> if all the tag names exist in the measurement, otherwise <see langword="false"/>.</returns>
    public bool MatchesTags(params string[] tags) => ContainsTags(Throw.IfNull(tags)) && (Tags.Count == tags.Length);

    internal string DebuggerToString() => $"{Value} @ {Timestamp.ToString("HH:mm:ss.ffff", CultureInfo.InvariantCulture)}";
}
