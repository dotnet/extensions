// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Classification;

/// <summary>
/// Represents a set of data classes.
/// </summary>
public sealed class DataClassificationSet : IEquatable<DataClassificationSet>
{
    private readonly HashSet<DataClassification> _classifications = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DataClassificationSet"/> class.
    /// </summary>
    /// <param name="classification">The class to include in the set.</param>
    public DataClassificationSet(DataClassification classification)
    {
        _ = _classifications.Add(classification);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataClassificationSet"/> class.
    /// </summary>
    /// <param name="classifications">The classes to include in the set.</param>
    public DataClassificationSet(IEnumerable<DataClassification> classifications)
    {
        _ = Throw.IfNull(classifications);
        _classifications.UnionWith(classifications);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataClassificationSet"/> class.
    /// </summary>
    /// <param name="classifications">The classes to include in the set.</param>
    public DataClassificationSet(params DataClassification[] classifications)
    {
        _ = Throw.IfNull(classifications);
        _classifications.UnionWith(classifications);
    }

    /// <summary>
    /// Converts a data classification to a data classification set.
    /// </summary>
    /// <param name="classification">The classification to include in the set.</param>
    public static implicit operator DataClassificationSet(DataClassification classification)
    {
        return FromDataClassification(classification);
    }

    /// <summary>
    /// Converts a data classification to a data classification set.
    /// </summary>
    /// <param name="classification">The classification to include in the set.</param>
    /// <returns>The resulting data classification set.</returns>
    public static DataClassificationSet FromDataClassification(DataClassification classification) => new(classification);

    /// <summary>
    /// Gets a hash code for the current object instance.
    /// </summary>
    /// <returns>The hash code value.</returns>
    public override int GetHashCode() => _classifications.GetHashCode();

    /// <summary>
    /// Compares an object with the current instance to see if they contain the same data classes.
    /// </summary>
    /// <param name="obj">The other data classification to compare to.</param>
    /// <returns><see langword="true"/> if the two sets contain the same classifications.</returns>
    public override bool Equals(object? obj)
    {
        var dc = obj as DataClassificationSet;
        if (dc == null)
        {
            return false;
        }

        return Equals(dc);
    }

    /// <summary>
    /// Compares an object with the current instance to see if they contain the same data classes.
    /// </summary>
    /// <param name="other">The other data classification to compare to.</param>
    /// <returns><see langword="true"/> if the two sets contain the same classifications.</returns>
    public bool Equals(DataClassificationSet? other)
    {
        if (other == null)
        {
            return false;
        }

        return _classifications.SetEquals(other._classifications);
    }

    /// <summary>
    /// Returns a string representation of this object.
    /// </summary>
    /// <returns>A string representation of this object.</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();

        foreach (var classification in _classifications)
        {
            if (sb.Length > 0)
            {
                _ = sb.Append(", ");
            }

            _ = sb.Append(classification.ToString());
        }

        return sb.ToString();
    }
}
