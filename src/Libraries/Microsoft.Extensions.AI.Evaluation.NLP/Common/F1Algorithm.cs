// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Common;

internal static class F1Algorithm
{
    public static double CalculateF1Score(IEnumerable<string> groundTruth, IEnumerable<string> response)
    {
        if (groundTruth == null || !groundTruth.Any())
        {
            Throw.ArgumentNullException(nameof(groundTruth), $"'{nameof(groundTruth)}' cannot be null or empty.");
        }

        if (response == null || !response.Any())
        {
            Throw.ArgumentNullException(nameof(response), $"'{nameof(response)}' cannot be null or empty.");
        }

        var referenceTokens = new MatchCounter<string>(groundTruth);
        var predictionTokens = new MatchCounter<string>(response);
        var commonTokens = referenceTokens.Intersect(predictionTokens);
        int numCommonTokens = commonTokens.Sum();

        if (numCommonTokens == 0)
        {
            return 0.0; // F1 score is 0 if there are no common tokens
        }
        else
        {
            double precision = (double)numCommonTokens / response.Count();
            double recall = (double)numCommonTokens / groundTruth.Count();
            double f1 = (2.0 * precision * recall) / (precision + recall);
            return f1;
        }
    }
}
