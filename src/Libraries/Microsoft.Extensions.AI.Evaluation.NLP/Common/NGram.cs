// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Common;

[DebuggerDisplay("{ToString(),nq}")]
[CollectionBuilder(typeof(NGramBuilder), nameof(NGramBuilder.Create))]
internal readonly struct NGram<T> : IEquatable<NGram<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    public NGram(ReadOnlySpan<T> values)
        : this(values.ToArray())
    {
    }

    public NGram(params T[] values)
    {
        Values = Throw.IfNull(values, nameof(values));
        _ = Throw.IfLessThan(values.Length, 1, nameof(values));
    }

    public readonly T[] Values { get; }

    public int Length => Values.Length;

    public bool Equals(NGram<T> other)
        => Values.SequenceEqual(other.Values);

    public override bool Equals(object? obj) => obj is NGram<T> other && Equals(other);

    public override int GetHashCode()
    {
        int hashCode = 0;
        foreach (var value in Values)
        {
            hashCode = HashCode.Combine(hashCode, value.GetHashCode());
        }

        return hashCode;
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => $"[{string.Join(",", Values.Select(v => v.ToString()))}]";

}

internal static class NGramBuilder
{
    public static NGram<T> Create<T>(ReadOnlySpan<T> values)
        where T : IEquatable<T> => new(values);
}
