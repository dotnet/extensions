// Assembly 'Microsoft.Extensions.Telemetry.Testing'

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Testing.Metering;

/// <summary>
/// Represents a single measurement performed by an instrument.
/// </summary>
/// <typeparam name="T">The type of metric measurement value.</typeparam>
[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
[DebuggerDisplay("{DebuggerToString(),nq}")]
public sealed class CollectedMeasurement<T> where T : struct
{
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
    /// <returns><see langword="true" /> if all the tags exist in the measurement with matching values, otherwise <see langword="false" />.</returns>
    public bool ContainsTags(params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Checks that the measurement includes a specific set of tags with any value.
    /// </summary>
    /// <param name="tags">The set of tag names to check.</param>
    /// <returns><see langword="true" /> if all the tags exist in the measurement, otherwise <see langword="false" />.</returns>
    public bool ContainsTags(params string[] tags);

    /// <summary>
    /// Checks that the measurement has an exactly matching set of tags with specific values.
    /// </summary>
    /// <param name="tags">The set of tags to check.</param>
    /// <returns><see langword="true" /> if all the tags exist in the measurement with matching values, otherwise <see langword="false" />.</returns>
    public bool MatchesTags(params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Checks that the measurement has a exactly matching set of tags with any value.
    /// </summary>
    /// <param name="tags">The set of tag names to check.</param>
    /// <returns><see langword="true" /> if all the tag names exist in the measurement, otherwise <see langword="false" />.</returns>
    public bool MatchesTags(params string[] tags);
}
