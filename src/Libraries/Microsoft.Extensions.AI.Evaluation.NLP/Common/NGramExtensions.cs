// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Common;

internal static class NGramExtensions
{
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

}
