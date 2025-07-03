// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.NLP.Common;
using Microsoft.Extensions.AI.Evaluation.Utilities;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.NLP;

/// <summary>
/// An <see cref="IEvaluator"/> that evaluates the quality of a response produced by an AI model by comparing
/// it to a reference response using the F1 scoring algorithm. F1 score is the ratio of the number of shared
/// words between the generated response and the reference response. 
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="F1Evaluator"/> computes the F1 score of a response ("hypothesis") in relation to a ground-truth reference
/// supplied by <see cref="F1EvaluatorContext.GroundTruth"/>. The score is returned in a <see cref="NumericMetric"/>
/// with a value between 0.0 and 1.0 where 0.0 represents no match at all and 1.0 indicates a perfect match.
/// By default, the score is interpreted with a pass/fail cutoff of 0.5. So a score of 0.5 or higher is
/// passing and a score below 0.5 is failing.
/// </para>
/// </remarks>
public sealed class F1Evaluator : IEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="F1Evaluator"/>.
    /// </summary>
    public static string F1MetricName => "F1";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> EvaluationMetricNames { get; } = [F1MetricName];

    /// <inheritdoc/>
    public ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(modelResponse);

        var metric = new NumericMetric(F1MetricName);
        var result = new EvaluationResult(metric);

        if (string.IsNullOrWhiteSpace(modelResponse.Text))
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error($"The {nameof(modelResponse)} supplied for evaluation was null or empty."));

            return new ValueTask<EvaluationResult>(result);
        }

        if (additionalContext?.OfType<F1EvaluatorContext>().FirstOrDefault()
                is not F1EvaluatorContext context)
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error(
                    $"A value of type '{nameof(F1EvaluatorContext)}' was not found in the '{nameof(additionalContext)}' collection."));

            return new ValueTask<EvaluationResult>(result);
        }

        (double score, TimeSpan duration) = TimingHelper.ExecuteWithTiming(() =>
        {
            string[] reference = SimpleWordTokenizer.WordTokenize(context.GroundTruth).ToArray();
            string[] hypothesis = SimpleWordTokenizer.WordTokenize(modelResponse.Text).ToArray();
            return F1Algorithm.CalculateF1Score(reference, hypothesis);
        });

        metric.Value = score;
        string durationText = $"{duration.TotalSeconds.ToString("F4", CultureInfo.InvariantCulture)} s";
        metric.AddOrUpdateMetadata(name: "evaluation-duration", value: durationText);
        metric.AddOrUpdateContext(context);
        metric.Interpretation = metric.Interpret();

        return new ValueTask<EvaluationResult>(result);
    }

}
