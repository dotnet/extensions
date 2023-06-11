// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Represents an event and the time it occurred relative to a well-known starting point.
/// </summary>
/// <remarks>
/// Related checkpoints are used to capture when sequential points in time are reached in an
/// operation like request execution. They are measured relative to the start of an operation and
/// hence capture latency as well as operation flow.
/// </remarks>
public readonly struct Checkpoint : IEquatable<Checkpoint>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Checkpoint"/> struct.
    /// </summary>
    /// <param name="name">Name of the checkpoint.</param>
    /// <param name="elapsed">Elapsed time since start.</param>
    /// <param name="frequency">Frequency of the elapsed time.</param>
    public Checkpoint(string name, long elapsed, long frequency)
    {
        Name = name;
        Elapsed = elapsed;
        Frequency = frequency;
    }

    /// <summary>
    /// Gets the name of the checkpoint.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the relative time since the beginning of the associated operation at which the checkpoint was created.
    /// </summary>
    public long Elapsed { get; }

    /// <summary>
    /// Gets the frequency of the timestamp value.
    /// </summary>
    public long Frequency { get; }

    /// <summary>
    /// Determines whether this and a specified object are identical.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> if identical;<see langword="false"/> otherwise.</returns>
    public override bool Equals(object? obj) => obj is Checkpoint m && Equals(m);

    /// <summary>
    /// Determines whether this and a specified checkpoint are identical.
    /// </summary>
    /// <param name="other">The other checkpoint.</param>
    /// <returns><see langword="true"/> if identical;<see langword="false"/> otherwise.</returns>
    public bool Equals(Checkpoint other)
        => Elapsed == other.Elapsed && Frequency == other.Frequency && Name.Equals(other.Name, StringComparison.Ordinal);

    /// <summary>
    /// Gets a hash code for this object.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
        => HashCode.Combine(Name, Elapsed, Frequency);

    /// <summary>
    /// Equality operator.
    /// </summary>
    /// <param name="left">First value.</param>
    /// <param name="right">Second value.</param>
    /// <returns><see langword="true" /> if the operands are equal, <see langword="false" /> otherwise.</returns>
    public static bool operator ==(Checkpoint left, Checkpoint right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    /// <param name="left">First value.</param>
    /// <param name="right">Second value.</param>
    /// <returns><see langword="true" /> if the operands are unequal, <see langword="false" /> otherwise.</returns>
    public static bool operator !=(Checkpoint left, Checkpoint right)
    {
        return !(left == right);
    }
}
