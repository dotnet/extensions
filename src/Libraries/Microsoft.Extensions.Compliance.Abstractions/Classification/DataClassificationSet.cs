// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Compliance.Classification;

/// <summary>
/// Represents a set of data classifications.
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
    /// Converts a single classification to a data classification set.
    /// </summary>
    /// <param name="classification">The classification to include in the set.</param>
    public static implicit operator DataClassificationSet(DataClassification classification)
    {
        return FromDataClassification(classification);
    }

    /// <summary>
    /// Converts a single classification to a data classification set.
    /// </summary>
    /// <param name="classification">The classification to include in the set.</param>
    /// <returns>The resulting data classification set.</returns>
    public static DataClassificationSet FromDataClassification(DataClassification classification) => new(classification);

    /// <summary>
    /// Creates a new data classification set that combines the current classifications with another set.
    /// </summary>
    /// <param name="other">The other set.</param>
    /// <remarks>
    /// This method doesn't modify the two input sets, it creates a new set.
    /// </remarks>
    /// <returns>The resulting set which combines the current instance's classifications and the other ones.</returns>
    public DataClassificationSet Union(DataClassificationSet other)
    {
        _ = Throw.IfNull(other);

        var result = new DataClassificationSet(other._classifications);
        result._classifications.UnionWith(_classifications);

        return result;
    }

    /// <summary>
    /// Gets a hash code for the current object instance.
    /// </summary>
    /// <returns>The hash code value.</returns>
    public override int GetHashCode() => _classifications.GetHashCode();

    /// <summary>
    /// Compares an object with the current instance to see if they contain the same classifications.
    /// </summary>
    /// <param name="obj">The other data classification to compare to.</param>
    /// <returns><see langword="true"/> if the two sets contain the same classifications.</returns>
    public override bool Equals(object? obj) => Equals(obj as DataClassificationSet);

    /// <summary>
    /// Compares an object with the current instance to see if they contain the same classifications.
    /// </summary>
    /// <param name="other">The other data classification to compare to.</param>
    /// <returns><see langword="true"/> if the two sets contain the same classifications.</returns>
    public bool Equals(DataClassificationSet? other) => other != null && _classifications.SetEquals(other._classifications);

    /// <summary>
    /// Returns a string representation of this object.
    /// </summary>
    /// <returns>The string representation of this object.</returns>
    public override string ToString()
    {
        var sb = PoolFactory.SharedStringBuilderPool.Get();

        var a = _classifications.ToArray();
        Array.Sort(a, (x, y) =>
        {
            var result = string.Compare(x.TaxonomyName, y.TaxonomyName, StringComparison.Ordinal);
            return result != 0 ? result : string.Compare(x.Value, y.Value, StringComparison.Ordinal);
        });

        _ = sb.Append('[');
        foreach (var c in a)
        {
            if (sb.Length > 1)
            {
                _ = sb.Append(',');
            }

            _ = sb.Append(c);
        }

        _ = sb.Append(']');
        var result = sb.ToString();
        PoolFactory.SharedStringBuilderPool.Return(sb);

        return result;
    }
}
