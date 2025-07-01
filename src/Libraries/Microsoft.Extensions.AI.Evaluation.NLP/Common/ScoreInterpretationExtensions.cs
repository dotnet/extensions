// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation.NLP.Common;

internal static class ScoreInterpretationExtensions
{
    internal static EvaluationMetricInterpretation Interpret(this NumericMetric metric)
    {
        // Many NLP scores range from 0.0 to 1.0, where:
        // - 0.0 means no match at all,
        // - 1.0 means a perfect match.
        // 0.5 is considered the minimum passing score for evaluation.

        EvaluationRating rating = metric.Value switch
        {
            null => EvaluationRating.Inconclusive,
            > 1.0 => EvaluationRating.Inconclusive,
            > 0.8 and <= 1.0 => EvaluationRating.Exceptional,
            > 0.6 and <= 0.8 => EvaluationRating.Good,
            > 0.4 and <= 0.6 => EvaluationRating.Average,
            > 0.2 and <= 0.4 => EvaluationRating.Poor,
            >= 0.0 and <= 0.2 => EvaluationRating.Unacceptable,
            < 0.0 => EvaluationRating.Inconclusive,
            _ => EvaluationRating.Inconclusive,
        };

        const double MinimumPassingScore = 0.5;
        return metric.Value is double value && value < MinimumPassingScore
            ? new EvaluationMetricInterpretation(
                rating,
                failed: true,
                reason: $"{metric.Name} is less than {MinimumPassingScore}.")
            : new EvaluationMetricInterpretation(rating);
    }
}
