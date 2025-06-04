// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.NLP.BLEU;

internal static class BLEUAlgorithm
{
    internal static IEnumerable<NGram<T>> NGrams<T>(IEnumerable<T> input, int n)
        where T : IEquatable<T>
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

    internal static int ClosestRefLength(IEnumerable<IEnumerable<string>> references, int hypLength)
    {
        if (!references.Any())
        {
            return 0;
        }

        int closestRefLength = 0;
        int closest = int.MaxValue;
        foreach (var reference in references)
        {
            int refLength = reference.Count();
            int diff = System.Math.Abs(refLength - hypLength);
            if (diff < closest ||
               (diff == closest && refLength < closestRefLength))
            {
                closest = diff;
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

        return System.Math.Exp(1 - ((double)closestRefLength / hypLength));
    }

    internal static RationalNumber ModifiedPrecision(IEnumerable<IEnumerable<string>> references, IEnumerable<string> hypothesis, int n = 1)
    {
        if (n <= 0)
        {
            Throw.ArgumentOutOfRangeException(nameof(n), "N must be greater than zero.");
        }

        if (!references.Any() || !hypothesis.Any())
        {
            return new RationalNumber(0, 0);
        }

        var hyp = NGrams(hypothesis, n).ToArray();
        var hypCounts = new MatchCounter<NGram<string>>(hyp);

        Dictionary<NGram<string>, int> maxCounts = [];

        var matchCounts = new MatchCounter<NGram<string>>();

        foreach (var rf in references)
        {
            var refGrams = NGrams(rf, n).ToArray();
            var refCounts = new MatchCounter<NGram<string>>(refGrams);

            foreach (var ct in refCounts.Values)
            {
                if (maxCounts.TryGetValue(ct.Key, out int val))
                {
                    maxCounts[ct.Key] = System.Math.Max(val, ct.Value);
                }
                else
                {
                    maxCounts[ct.Key] = ct.Value;
                }
            }
        }

        Dictionary<NGram<string>, int> clippedCounts = [];
        foreach (var h in hypCounts.Values)
        {
            if (maxCounts.TryGetValue(h.Key, out var v))
            {
                clippedCounts[h.Key] = System.Math.Min(h.Value, v);
            }
            else
            {
                // If the hypothesis n-gram is not in any reference, it is clipped to 0.
                clippedCounts[h.Key] = 0;
            }
        }

        int numerator = clippedCounts.Values.Sum();
        int denominator = System.Math.Max(1, hypCounts.Sum);

        return new RationalNumber(numerator, denominator);
    }

    internal static double[] EqualWeights(int n)
    {
        if (n <= 0)
        {
            Throw.ArgumentOutOfRangeException(nameof(n), "N must be greater than zero.");
        }

        double[] weights = new double[n];
        for (int i = 0; i < n; i++)
        {
            weights[i] = 1.0 / n;
        }

        return weights;
    }

    internal static readonly double[] DefaultBLEUWeights = EqualWeights(4);

    internal static double SentenceBLEU(IEnumerable<IEnumerable<string>> references, IEnumerable<string> hypothesis)
        => SentenceBLEU(references, hypothesis, DefaultBLEUWeights);

    internal static double SentenceBLEU(IEnumerable<IEnumerable<string>> references, IEnumerable<string> hypothesis, double[] weights,
        Func<RationalNumber[], int, double[]>? smoothingFunction = null)
    {
        if (references == null || !references.Any())
        {
            Throw.ArgumentNullException(nameof(references), "References cannot be null or empty.");
        }

        if (hypothesis == null || !hypothesis.Any())
        {
            Throw.ArgumentNullException(nameof(hypothesis), "Hypothesis cannot be null or empty.");
        }

        if (weights == null || !weights.Any())
        {
            Throw.ArgumentNullException(nameof(weights), "Weights cannot be null or empty.");
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

        int hypLen = hypothesis.Count();
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
                score += weights[i] * System.Math.Log(smoothedValues[i]);
            }
        }

        return brevityPenalty * System.Math.Exp(score);
    }

}
