// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.NLP.BLEU;

internal readonly struct MatchCounter<T>
    where T : IEquatable<T>
{
    private readonly Dictionary<T, int> _counts = [];

    public readonly IEnumerable<KeyValuePair<T, int>> Values => _counts;

    public readonly int Sum => _counts.Values.Sum();

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
        foreach (var item in items)
        {
            Add(item);
        }
    }

    public override string ToString() => string.Concat(Values.Select(v => $"{v.Key}: {v.Value}, "));
}
