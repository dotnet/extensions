// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Common;

internal static class NGramExtensions
{
    // Collection builder method.
    public static NGram<T> CreateNGram<T>(this ReadOnlySpan<T> values)
        where T : IEquatable<T> => new(values);

    internal static List<NGram<T>> CreateNGrams<T>(this Span<T> input, int n)
        where T : IEquatable<T>
        => CreateNGrams((ReadOnlySpan<T>)input, n);

    /// <summary>
    /// Create a sequence of n-grams from the input sequence.
    /// </summary>
    /// <param name="input">The input sequence of items.</param>
    /// <param name="n">The size of each n-gram.</param>
    internal static List<NGram<T>> CreateNGrams<T>(this ReadOnlySpan<T> input, int n)
        where T : IEquatable<T>
    {
        if (n <= 0)
        {
            Throw.ArgumentOutOfRangeException(nameof(n), $"'{nameof(n)}' must be greater than zero.");
        }

        List<NGram<T>> nGrams = [];

        ReadOnlySpan<T> output = input.Slice(0, Math.Min(n, input.Length));

        while (output.Length == n)
        {
            nGrams.Add(new NGram<T>(output));

            input = input.Slice(1);
            output = input.Slice(0, Math.Min(n, input.Length));
        }

        return nGrams;
    }

    internal static List<NGram<T>> CreateAllNGrams<T>(this Span<T> input, int minN, int maxN = -1)
        where T : IEquatable<T>
        => CreateAllNGrams((ReadOnlySpan<T>)input, minN, maxN);

    /// <summary>
    /// Create a sequence of all n-grams from the input sequence from minN to maxN.
    /// </summary>
    /// <param name="input">The input sequence of items.</param>
    /// <param name="minN">The minimum size of n-gram.</param>
    /// <param name="maxN">The maximum size of n-gram. If not specified, the default is to include up to length of the input.</param>
    internal static List<NGram<T>> CreateAllNGrams<T>(this ReadOnlySpan<T> input, int minN, int maxN = -1)
        where T : IEquatable<T>
    {
        _ = Throw.IfLessThanOrEqual(minN, 0, nameof(minN));

        if (maxN < 0)
        {
            maxN = input.Length; // Update to use Length instead of Count()
        }
        else if (maxN < minN)
        {
            Throw.ArgumentOutOfRangeException(nameof(maxN), $"'{nameof(maxN)}' must be greater than or equal to '{nameof(minN)}'.");
        }

        List<NGram<T>> nGrams = [];

        for (int i = 0; i <= input.Length - minN; i++)
        {
            for (int s = minN; s <= maxN && s <= input.Length - i; s++)
            {
                nGrams.Add(new NGram<T>(input.Slice(i, s)));
            }
        }

        return nGrams;
    }
}

