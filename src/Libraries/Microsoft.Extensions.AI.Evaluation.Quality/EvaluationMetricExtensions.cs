// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation.Quality;

internal static class EvaluationMetricExtensions
{
    internal static EvaluationMetricInterpretation InterpretScore(this NumericMetric metric)
    {
        EvaluationRating rating = metric.Value switch
        {
            null => EvaluationRating.Inconclusive,
            > 5.0 => EvaluationRating.Inconclusive,
            > 4.0 and <= 5.0 => EvaluationRating.Exceptional,
            > 3.0 and <= 4.0 => EvaluationRating.Good,
            > 2.0 and <= 3.0 => EvaluationRating.Average,
            > 1.0 and <= 2.0 => EvaluationRating.Poor,
            > 0.0 and <= 1.0 => EvaluationRating.Unacceptable,
            <= 0.0 => EvaluationRating.Inconclusive,
            _ => EvaluationRating.Inconclusive,
        };

        const double MinimumPassingScore = 4.0;
        return metric.Value is double value && value < MinimumPassingScore
            ? new EvaluationMetricInterpretation(
                rating,
                failed: true,
                reason: $"{metric.Name} is less than {MinimumPassingScore}.")
            : new EvaluationMetricInterpretation(rating);
    }
}
