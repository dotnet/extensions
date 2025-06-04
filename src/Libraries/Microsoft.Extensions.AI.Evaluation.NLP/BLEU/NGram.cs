// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.NLP.BLEU;

[CollectionBuilder(typeof(NGramBuilder), nameof(NGramBuilder.Create))]
internal readonly struct NGram<T> : IEquatable<NGram<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    /// <summary>
    /// Create a sequence of n-grams from the input sequence.
    /// </summary>
    /// <param name="input">The input sequence of items.</param>
    /// <param name="n">The size of each n-gram.</param>
    internal static IEnumerable<NGram<T>> Create(IEnumerable<T> input, int n)
    {
        if (n <= 0)
        {
            Throw.ArgumentOutOfRangeException(nameof(n), "N must be greater than zero.");
        }

        var output = input.Take(n).ToArray();

        while (output.Length == n)
        {
            yield return new NGram<T>(output);

            input = input.Skip(1);
            output = input.Take(n).ToArray();
        }
    }

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
    {
        if (other.Length != Length)
        {
            return false;
        }

        for (int i = 0; i < Length; i++)
        {
            if (!Values[i].Equals(other.Values[i]))
            {
                return false;
            }
        }

        return true;
    }

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

