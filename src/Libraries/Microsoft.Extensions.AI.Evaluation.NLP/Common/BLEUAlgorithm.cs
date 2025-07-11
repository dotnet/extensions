﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Common;

/// <summary>
/// Helper methods for calculating the BLEU score.
/// See <see href="https://en.wikipedia.org/wiki/BLEU">BLEU on Wikipedia</see> or
/// <see href="https://github.com/nltk/nltk/blob/develop/nltk/translate/bleu_score.py">NLTK implementation</see>
/// for more details.
/// </summary>
internal static class BLEUAlgorithm
{
    internal static int ClosestRefLength(string[][] references, int hypLength)
    {
        if (!references.Any())
        {
            return 0;
        }

        int closestRefLength = 0;
        int smallestDiff = int.MaxValue;
        foreach (var reference in references)
        {
            int refLength = reference.Length;
            int diff = Math.Abs(refLength - hypLength);
            if (diff < smallestDiff ||
               (diff == smallestDiff && refLength < closestRefLength))
            {
                smallestDiff = diff;
                closestRefLength = refLength;
            }
        }

        return closestRefLength;
    }

    internal static double BrevityPenalty(int closestRefLength, int hypLength)
    {
        if (hypLength <= 0)
        {
            return 0.0;
        }

        if (closestRefLength <= 0 || hypLength > closestRefLength)
        {
            return 1.0;
        }

        return Math.Exp(1 - ((double)closestRefLength / hypLength));
    }

    internal static RationalNumber ModifiedPrecision(string[][] references, string[] hypothesis, int n = 1)
    {
        if (n <= 0)
        {
            Throw.ArgumentOutOfRangeException(nameof(n), $"`{nameof(n)}` must be greater than zero.");
        }

        if (references.Length == 0 || hypothesis.Length == 0)
        {
            return RationalNumber.Zero;
        }

        List<NGram<string>> hypGrams = hypothesis.CreateNGrams(n);
        MatchCounter<NGram<string>> hypCounts = new(hypGrams);

        Dictionary<NGram<string>, int> maxCounts = [];

        foreach (var rf in references)
        {
            List<NGram<string>> refGrams = rf.CreateNGrams(n);
            MatchCounter<NGram<string>> refCounts = new(refGrams);

            foreach (var ct in refCounts)
            {
                if (maxCounts.TryGetValue(ct.Key, out int val))
                {
                    maxCounts[ct.Key] = Math.Max(val, ct.Value);
                }
                else
                {
                    maxCounts[ct.Key] = ct.Value;
                }
            }
        }

        Dictionary<NGram<string>, int> clippedCounts = [];
        foreach (var h in hypCounts)
        {
            if (maxCounts.TryGetValue(h.Key, out var v))
            {
                clippedCounts[h.Key] = Math.Min(h.Value, v);
            }
            else
            {
                // If the hypothesis n-gram is not in any reference, it is clipped to 0.
                clippedCounts[h.Key] = 0;
            }
        }

        int numerator = clippedCounts.Values.Sum();
        int denominator = Math.Max(1, hypCounts.Sum());

        return new RationalNumber(numerator, denominator);
    }

    /// <summary>
    /// Generate an n-sized array of equal weights that sum to 1.0.
    /// </summary>
    /// <param name="n">Number of weights to return.</param>
    /// <returns>Array of equal sized values that sum to 1.0.</returns>
    internal static double[] EqualWeights(int n)
    {
        if (n <= 0)
        {
            Throw.ArgumentOutOfRangeException(nameof(n), $"'{nameof(n)}' must be greater than zero.");
        }

        double[] weights = new double[n];
#if NET8_0_OR_GREATER
        Array.Fill(weights, 1.0 / n);
#else
        for (int i = 0; i < n; i++)
        {
            weights[i] = 1.0 / n;
        }
#endif
        return weights;
    }

    internal static readonly double[] DefaultBLEUWeights = EqualWeights(4);

    internal static double SentenceBLEU(string[][] references, string[] hypothesis,
        double[]? weights = null, Func<RationalNumber[], int, double[]>? smoothingFunction = null)
    {
        if (references == null || references.Length == 0)
        {
            Throw.ArgumentNullException(nameof(references), $"'{nameof(references)}' cannot be null or empty.");
        }

        if (hypothesis == null || hypothesis.Length == 0)
        {
            Throw.ArgumentNullException(nameof(hypothesis), $"'{nameof(hypothesis)}' cannot be null or empty.");
        }

        if (weights is null)
        {
            weights = DefaultBLEUWeights;
        }

        if (weights.Length == 0)
        {
            Throw.ArgumentNullException(nameof(weights), $"'{nameof(weights)}' cannot be empty.");
        }

        var precisionValues = new RationalNumber[weights.Length];
        for (int i = 0; i < weights.Length; i++)
        {
            int n = i + 1;
            RationalNumber prec = ModifiedPrecision(references, hypothesis, n);

            if (i == 0 && prec.Numerator == 0)
            {
                // If the precision for unigrams (n == 1) is zero, the there can be no higher order matches and BLEU score is zero.
                return 0.0;
            }

            precisionValues[i] = prec;
        }

        int hypLen = hypothesis.Length;
        int closestRefLength = ClosestRefLength(references, hypLen);
        double brevityPenalty = BrevityPenalty(closestRefLength, hypLen);

        if (smoothingFunction == null)
        {
            smoothingFunction = SmoothingFunction.Method0;
        }

        double[] smoothedValues = smoothingFunction(precisionValues, hypLen);

        double score = 0.0;
        for (int i = 0; i < weights.Length; i++)
        {
            if (smoothedValues[i] > 0)
            {
                score += weights[i] * Math.Log(smoothedValues[i]);
            }
        }

        return brevityPenalty * Math.Exp(score);
    }

}
