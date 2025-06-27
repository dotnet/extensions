// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.NLP.Common;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.NLP;

/// <summary>
/// An <see cref="IEvaluator"/> that evaluates the quality of a response produced by an AI model by comparing
/// it to a reference response using the GLEU (Google-BLEU) algorithm. The GLEU evaluator measures the similarity
/// between the generated response and one or more reference responses using n-gram overlap.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="GLEUEvaluator"/> computes the GLEU score of a response ("hypothesis") compared to a reference
/// <see cref="GLEUEvaluatorContext.References"/>. The score is returned in a <see cref="NumericMetric"/>
/// with a value between 0.0 and 1.0 where 0.0 represents no match at all and 1.0 indicates a perfect match.
/// By default, the score is interpreted with a pass/fail cutoff of 0.5. So a score of 0.5 or higher is
/// passing and a score below 0.5 is failing.
/// </para>
/// </remarks>
[Experimental("AIEVAL001")]
public sealed class GLEUEvaluator : IEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="GLEUEvaluator"/>.
    /// </summary>
    public static string GLEUMetricName => "GLEU";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> EvaluationMetricNames { get; } = [GLEUMetricName];

    /// <inheritdoc/>
    public ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        _ = Throw.IfNull(modelResponse);

        var metric = new NumericMetric(GLEUMetricName);
        var result = new EvaluationResult(metric);

        if (string.IsNullOrWhiteSpace(modelResponse.Text))
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error($"The {nameof(modelResponse)} supplied for evaluation was null or empty."));

            return new ValueTask<EvaluationResult>(result);
        }

        if (additionalContext?.OfType<GLEUEvaluatorContext>().FirstOrDefault()
                is not GLEUEvaluatorContext context)
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error(
                    $"A value of type '{nameof(GLEUEvaluatorContext)}' was not found in the '{nameof(additionalContext)}' collection."));

            return new ValueTask<EvaluationResult>(result);
        }

        var references = context.References.Select(reference => SimpleWordTokenizer.WordTokenize(reference));
        var hypothesis = SimpleWordTokenizer.WordTokenize(modelResponse.Text);
        metric.Value = GLEUAlgorithm.SentenceGLEU(references, hypothesis);

        stopwatch.Stop();
        string durationText = $"{stopwatch.Elapsed.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture)} s";

        metric.AddOrUpdateContext(context);
        metric.AddOrUpdateMetadata(name: "evaluation-duration", value: durationText);
        metric.Interpretation = InterpretScore(metric);

        return new ValueTask<EvaluationResult>(result);
    }

    private static EvaluationMetricInterpretation InterpretScore(NumericMetric metric)
    {
        // GLEU scores range from 0.0 to 1.0, where:
        // - 0.0 means no match at all,
        // - 1.0 means a perfect match.
        // 0.5 is considered the minimum passing score for GLEU evaluation.

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
