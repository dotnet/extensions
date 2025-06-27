// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Common;

internal static class NGramExtensions
{
    // Collection builder method.
    public static NGram<T> CreateNGram<T>(this ReadOnlySpan<T> values)
        where T : IEquatable<T> => new(values);

    /// <summary>
    /// Create a sequence of n-grams from the input sequence.
    /// </summary>
    /// <param name="input">The input sequence of items.</param>
    /// <param name="n">The size of each n-gram.</param>
    internal static IEnumerable<NGram<T>> CreateNGrams<T>(this IEnumerable<T> input, int n)
        where T : IEquatable<T>
    {
        if (n <= 0)
        {
            Throw.ArgumentOutOfRangeException(nameof(n), $"'{nameof(n)}' must be greater than zero.");
        }

        T[] output = [.. input.Take(n)];

        while (output.Length == n)
        {
            yield return new NGram<T>(output);

            input = input.Skip(1);
            output = [.. input.Take(n)];
        }
    }

    /// <summary>
    /// Create a sequence of all n-grams from the input sequence from minN to maxN.
    /// </summary>
    /// <param name="input">The input sequence of items.</param>
    /// <param name="minN">The minimum size of n-gram.</param>
    /// <param name="maxN">The maximum size of n-gram. If not specified, the default is to include up to length of the input.</param>
    internal static IEnumerable<NGram<T>> CreateAllNGrams<T>(this IEnumerable<T> input, int minN, int maxN = -1)
        where T : IEquatable<T>
    {
        _ = Throw.IfNull(input, nameof(input));
        _ = Throw.IfLessThanOrEqual(minN, 0, nameof(minN));

        if (maxN < 0)
        {
            maxN = input.Count();
        }
        else if (maxN < minN)
        {
            Throw.ArgumentOutOfRangeException(nameof(maxN), $"'{nameof(maxN)}' must be greater than or equal to '{nameof(minN)}'.");
        }

        // Capture input
        T[] tokens = input.ToArray();

        for (int i = 0; i <= tokens.Length - minN; i++)
        {
            for (int s = minN; s <= maxN && s <= tokens.Length - i; s++)
            {
                yield return [.. tokens.AsSpan().Slice(i, s)];
            }
        }
    }
}
