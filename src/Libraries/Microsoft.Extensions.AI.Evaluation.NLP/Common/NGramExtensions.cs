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

    internal static IEnumerable<NGram<T>> CreateNGrams<T>(this Span<T> input, int n)
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

}
