// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AsyncState;

/// <summary>
/// Async state token representing a registered context within the asynchronous state.
/// </summary>
public readonly struct AsyncStateToken : IEquatable<AsyncStateToken>
{
    internal AsyncStateToken(int index)
    {
        Index = index;
    }

    internal readonly int Index { get; }

    /// <summary>
    /// Determines whether the specified object is equal to the current async state token.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true"/> if the specified object is identical to the current async state token; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj)
    {
        return obj is AsyncStateToken token && Equals(token);
    }

    /// <summary>
    /// Determines whether this async state token and a specified async state token are identical.
    /// </summary>
    /// <param name="other">The other async state token.</param>
    /// <returns><see langword="true"/> if the two async state tokens are identical; otherwise, <see langword="false"/>.</returns>
    public bool Equals(AsyncStateToken other)
    {
        return Index == other.Index;
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode()
    {
        return Index.GetHashCode();
    }

    /// <summary>
    /// Compares two instances.
    /// </summary>
    /// <param name="left">Left argument of the comparison.</param>
    /// <param name="right">Right argument of the comparison.</param>
    /// <returns><see langword="true" /> when equal, <see langword="false" /> otherwise.</returns>
    public static bool operator ==(AsyncStateToken left, AsyncStateToken right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two instances.
    /// </summary>
    /// <param name="left">Left argument of the comparison.</param>
    /// <param name="right">Right argument of the comparison.</param>
    /// <returns><see langword="true" /> when not equal, <see langword="false" /> otherwise.</returns>
    public static bool operator !=(AsyncStateToken left, AsyncStateToken right)
    {
        return !(left == right);
    }
}
