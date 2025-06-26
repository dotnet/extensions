// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Common;

/// <summary>
/// Implementations of smoothing functions for BLEU scores taken from
/// `A Systematic Comparison of Smoothing Techniques for Sentence-Level BLEU` 
/// by Chen and Cherry. http://acl2014.org/acl2014/W14-33/pdf/W14-3346.pdf.
/// </summary>
internal static class SmoothingFunction
{
    /// <summary>
    /// This is the baseline method, which does not apply any smoothing.
    /// </summary>
    /// <param name="precisions">N precision values to be smoothed.</param>
    /// <param name="hypLen">Number of tokens in the hypothesis.</param>
    /// <returns>Smoothed precision values.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Matches expected signature of SmoothingFunction")]
    internal static double[] Method0(RationalNumber[] precisions, int hypLen)
    {
        double[] smoothed = new double[precisions.Length];
        for (int i = 0; i < precisions.Length; i++)
        {
            if (precisions[i].Numerator == 0)
            {
                smoothed[i] = double.Epsilon;
            }
            else
            {
                smoothed[i] = precisions[i].ToDouble();
            }
        }

        return smoothed;
    }

    /// <summary>
    /// Smoothing method 4:
    /// Shorter translations may have inflated precision values due to having
    /// smaller denominators; therefore, we give them proportionally
    /// smaller smoothed counts. Instead of scaling to 1/(2^k), Chen and Cherry
    /// suggests dividing by 1/ln(len(T)), where T is the length of the translation.
    /// </summary>
    /// <param name="precisions">N precision values to be smoothed.</param>
    /// <param name="hypLen">Number of tokens in the hypothesis.</param>
    /// <returns>Smoothed precision values.</returns>
    internal static double[] Method4(RationalNumber[] precisions, int hypLen)
    {
        const double DefaultK = 5.0;

        double[] smoothed = new double[precisions.Length];

        int incvnt = 0;
        for (int i = 0; i < precisions.Length; i++)
        {
            RationalNumber p = precisions[i];
            if (precisions[i].Numerator == 0 && hypLen > 1)
            {
                double numerator = 1 / (Math.Pow(2.0, incvnt) * DefaultK / Math.Log(hypLen));
                incvnt++;
            }
            else
            {
                smoothed[i] = p.ToDouble();
            }
        }

        return smoothed;
    }
}
