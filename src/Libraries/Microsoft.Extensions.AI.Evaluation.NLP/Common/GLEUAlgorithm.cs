// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Common;

/// <summary>
/// Google-BLEU (GLEU) algorithm implementation for evaluating the quality of a response.
/// Python implementation reference: https://www.nltk.org/api/nltk.translate.gleu_score.html.
/// </summary>
internal static class GLEUAlgorithm
{
    internal static double SentenceGLEU(string[][] references, string[] hypothesis, int minN = 1, int maxN = 4)
    {
        if (references == null || references.Length == 0)
        {
            Throw.ArgumentNullException(nameof(references), $"'{nameof(references)}' cannot be null or empty.");
        }

        if (hypothesis == null || hypothesis.Length == 0)
        {
            Throw.ArgumentNullException(nameof(hypothesis), $"'{nameof(hypothesis)}' cannot be null or empty.");
        }

        MatchCounter<NGram<string>> hypNGrams = new(hypothesis.CreateAllNGrams(minN, maxN));
        int truePosFalsePos = hypNGrams.Sum();

        List<(int, int)> hypCounts = [];
        foreach (var reference in references)
        {
            MatchCounter<NGram<string>> refNGrams = new(reference.CreateAllNGrams(minN, maxN));
            int truePosFalseNeg = refNGrams.Sum();

            MatchCounter<NGram<string>> overlapNGrams = hypNGrams.Intersect(refNGrams);
            int truePos = overlapNGrams.Sum();

            int nAll = Math.Max(truePosFalsePos, truePosFalseNeg);

            if (nAll > 0)
            {
                hypCounts.Add((truePos, nAll));
            }
        }

        int corpusNMatch = 0;
        int corpusNAll = 0;

        foreach (var (truePos, nAll) in hypCounts)
        {
            corpusNMatch += truePos;
            corpusNAll += nAll;
        }

        if (corpusNAll == 0)
        {
            return 0.0;
        }
        else
        {
            return (double)corpusNMatch / corpusNAll;
        }
    }
}
