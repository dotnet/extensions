// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET8_0_OR_GREATER

using System;
using Microsoft.Shared.Diagnostics;

#pragma warning disable CA1716
namespace Microsoft.Shared.StringSplit;
#pragma warning restore CA1716

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal readonly struct StringRange : IComparable, IComparable<StringRange>, IEquatable<StringRange>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringRange"/> struct.
    /// </summary>
    /// <param name="index">Starting index of the segment.</param>
    /// <param name="count">Number of characters in the segment.</param>
    public StringRange(int index, int count)
    {
        _ = Throw.IfLessThan(index, 0);
        _ = Throw.IfLessThan(count, 0);

        Index = index;
        Count = count;
    }

    /// <summary>
    /// Gets the starting index of the string.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the number of characters in the segment.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Compare current instance of <see cref="StringRange"/> to another.
    /// </summary>
    /// <param name="other">Segment to compare.</param>
    /// <returns>
    /// Returns a value less than zero if this less than other, zero if this equal to other,
    /// or a value greater than zero if this greater than other.
    /// </returns>
    public int CompareTo(StringRange other) => Index.CompareTo(other.Index);

    /// <summary>
    /// Compare current instance of <see cref="StringRange"/> to another object.
    /// </summary>
    /// <param name="obj">Segment to compare.</param>
    /// <returns>
    /// Returns a value less than zero if this less than other, zero if this equal to other,
    /// or a value greater than zero if this greater than other.
    /// Null is considered to be less than any instance.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If object is not of same type.
    /// </exception>
    public int CompareTo(object? obj)
    {
        if (obj is StringRange ss)
        {
            return CompareTo(ss);
        }

        if (obj != null)
        {
            Throw.ArgumentException(nameof(obj), $"Provided value must be of type {typeof(StringRange)}, but was of type {obj.GetType()}.");
        }

        return 1;
    }

    /// <summary>
    /// Compares two string segments.
    /// </summary>
    /// <param name="other">Segment to compare.</param>
    /// <returns><see langword="true" /> when equal, <see langword="false" /> otherwise.</returns>
    public bool Equals(StringRange other) => other.Index == Index && other.Count == Count;

    /// <summary>
    /// Compares two string segments.
    /// </summary>
    /// <param name="obj">Segment to compare.</param>
    /// <returns><see langword="true" /> when equal, <see langword="false" /> otherwise.</returns>
    public override bool Equals(object? obj) => obj is StringRange ss && Equals(ss);

    /// <summary>
    /// Returns the hashcode for this instance.
    /// </summary>
    /// <returns>Hash code.</returns>
    public override int GetHashCode() => HashCode.Combine(Index, Count);

    /// <summary>
    /// Compares two string segments.
    /// </summary>
    /// <param name="left">Left segment to compare.</param>
    /// <param name="right">Right segment to compare.</param>
    /// <returns><see langword="true" /> when equal, <see langword="false" /> otherwise.</returns>
    public static bool operator ==(StringRange left, StringRange right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two string segments.
    /// </summary>
    /// <param name="left">Left segment to compare.</param>
    /// <param name="right">Right segment to compare.</param>
    /// <returns><see langword="true" /> when not equal, <see langword="false" /> otherwise.</returns>
    public static bool operator !=(StringRange left, StringRange right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Compares two string segments.
    /// </summary>
    /// <param name="left">Left segment to compare.</param>
    /// <param name="right">Right segment to compare.</param>
    /// <returns><see langword="true" /> when first segment is before the second, <see langword="false" /> otherwise.</returns>
    public static bool operator <(StringRange left, StringRange right)
    {
        return left.Index < right.Index;
    }

    /// <summary>
    /// Compares two string segments.
    /// </summary>
    /// <param name="left">Left segment to compare.</param>
    /// <param name="right">Right segment to compare.</param>
    /// <returns><see langword="true" /> when first segment is after the second, <see langword="false" /> otherwise.</returns>
    public static bool operator >(StringRange left, StringRange right)
    {
        return left.Index > right.Index;
    }

    /// <summary>
    /// Compares two string segments.
    /// </summary>
    /// <param name="left">Left segment to compare.</param>
    /// <param name="right">Right segment to compare.</param>
    /// <returns><see langword="true" /> when first segment is before or at the same index as the second, <see langword="false" /> otherwise.</returns>
    public static bool operator <=(StringRange left, StringRange right)
    {
        return left.Index <= right.Index;
    }

    /// <summary>
    /// Compares two string segments.
    /// </summary>
    /// <param name="left">Left segment to compare.</param>
    /// <param name="right">Right segment to compare.</param>
    /// <returns><see langword="true" /> when first segment is at the same index or after the second, <see langword="false" /> otherwise.</returns>
    public static bool operator >=(StringRange left, StringRange right)
    {
        return left.Index >= right.Index;
    }
}

#endif
