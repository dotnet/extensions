// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
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

        var (score, duration) = TimingHelper.ExecuteWithTiming(() =>
        {
            var references = context.References.Select(reference => SimpleWordTokenizer.WordTokenize(reference).ToArray()).ToArray();
            var hypothesis = SimpleWordTokenizer.WordTokenize(modelResponse.Text).ToArray();
            return GLEUAlgorithm.SentenceGLEU(references, hypothesis);
        });

        metric.Value = score;
        string durationText = $"{duration.TotalSeconds.ToString("F2", CultureInfo.InvariantCulture)} s";
        metric.AddOrUpdateMetadata(name: "evaluation-duration", value: durationText);
        metric.AddOrUpdateContext(context);
        metric.Interpretation = NLPScoreInterpretation.Interpret(metric);

        return new ValueTask<EvaluationResult>(result);
    }

}
