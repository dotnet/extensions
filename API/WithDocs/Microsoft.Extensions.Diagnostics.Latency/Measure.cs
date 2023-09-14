// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.Latency;

/// <summary>
/// Represents a measure.
/// </summary>
/// <remarks>
/// Measures are used to aggregate or record values. They are used to track
/// statistics about recurring operations. Example: number of calls to
/// a database, total latency of database calls etc.
/// </remarks>
public readonly struct Measure : IEquatable<Measure>
{
    /// <summary>
    /// Gets the name of the measure.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the value of the measure.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Diagnostics.Latency.Measure" /> struct.
    /// </summary>
    /// <param name="name">Name of the counter.</param>
    /// <param name="value">Value of the counter.</param>
    public Measure(string name, long value);

    /// <summary>
    /// Determines whether this and a specified object are identical.
    /// </summary>
    /// /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true" /> if identical;<see langword="false" /> otherwise.</returns>
    public override bool Equals(object? obj);

    /// <summary>
    /// Determines whether this and a specified measure are identical.
    /// </summary>
    /// <param name="other">The other measure.</param>
    /// <returns><see langword="true" /> if identical;<see langword="false" /> otherwise.</returns>
    public bool Equals(Measure other);

    /// <summary>
    /// Gets a hash code for this object.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode();

    /// <summary>
    /// Equality operator.
    /// </summary>
    /// <param name="left">First value.</param>
    /// <param name="right">Second value.</param>
    /// <returns><see langword="true" /> if the operands are equal, <see langword="false" /> otherwise.</returns>
    public static bool operator ==(Measure left, Measure right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    /// <param name="left">First value.</param>
    /// <param name="right">Second value.</param>
    /// <returns><see langword="true" /> if the operands are unequal, <see langword="false" /> otherwise.</returns>
    public static bool operator !=(Measure left, Measure right);
}
