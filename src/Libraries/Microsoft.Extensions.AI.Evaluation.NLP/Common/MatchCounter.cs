// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Common;

[DebuggerDisplay("{ToDebugString(),nq}")]
internal readonly struct MatchCounter<T> : IEnumerable<KeyValuePair<T, int>>
    where T : IEquatable<T>
{
    private readonly Dictionary<T, int> _counts = [];

    public readonly int Sum() => _counts.Values.Sum();

    public MatchCounter()
    {
    }

    public MatchCounter(IEnumerable<T> items)
    {
        _ = Throw.IfNull(items, nameof(items));
        AddRange(items);
    }

    public void Add(T item)
    {
        if (_counts.TryGetValue(item, out int currentCount))
        {
            _counts[item] = currentCount + 1;
        }
        else
        {
            _counts[item] = 1;
        }
    }

    public void AddRange(IEnumerable<T> items)
    {
        if (items == null)
        {
            return;
        }

        foreach (var item in items)
        {
            Add(item);
        }
    }

    public MatchCounter<T> Intersect(MatchCounter<T> other)
    {
        _ = Throw.IfNull(other, nameof(other));
        var intersection = new MatchCounter<T>();

        (Dictionary<T, int> smaller, Dictionary<T, int> larger) =
            _counts.Count < other._counts.Count ? (_counts, other._counts) : (other._counts, _counts);

        foreach (var kvp in smaller)
        {
            if (larger.TryGetValue(kvp.Key, out int otherCount))
            {
                intersection._counts[kvp.Key] = Math.Min(kvp.Value, otherCount);
            }
        }

        return intersection;
    }

    public string ToDebugString() => string.Join(",", _counts.Select(v => $"{v.Key}: {v.Value}"));

    public IEnumerator<KeyValuePair<T, int>> GetEnumerator() => _counts.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_counts).GetEnumerator();
}
