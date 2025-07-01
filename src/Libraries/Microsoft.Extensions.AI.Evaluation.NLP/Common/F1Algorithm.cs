// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Common;

/// <summary>
/// F1 score for a response is the ratio of the number of shared words between the generated response
/// and the reference response. Python implementation reference
/// https://github.com/Azure/azure-sdk-for-python/blob/main/sdk/evaluation/azure-ai-evaluation/azure/ai/evaluation/_evaluators/_f1_score/_f1_score.py.
/// </summary>
internal static class F1Algorithm
{
    public static double CalculateF1Score(string[] groundTruth, string[] response)
    {
        if (groundTruth == null || groundTruth.Length == 0)
        {
            Throw.ArgumentNullException(nameof(groundTruth), $"'{nameof(groundTruth)}' cannot be null or empty.");
        }

        if (response == null || response.Length == 0)
        {
            Throw.ArgumentNullException(nameof(response), $"'{nameof(response)}' cannot be null or empty.");
        }

        MatchCounter<string> referenceTokens = new(groundTruth);
        MatchCounter<string> predictionTokens = new(response);
        MatchCounter<string> commonTokens = referenceTokens.Intersect(predictionTokens);
        int numCommonTokens = commonTokens.Sum();

        if (numCommonTokens == 0)
        {
            return 0.0; // F1 score is 0 if there are no common tokens
        }
        else
        {
            double precision = (double)numCommonTokens / response.Length;
            double recall = (double)numCommonTokens / groundTruth.Length;
            double f1 = (2.0 * precision * recall) / (precision + recall);
            return f1;
        }
    }
}
