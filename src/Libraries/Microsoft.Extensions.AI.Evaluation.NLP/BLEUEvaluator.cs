﻿// Licensed to the .NET Foundation under one or more agreements.
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
/// it to a reference response using the BLEU (Bilingual Evaluation Understudy) algorithm. It is often used
/// to evaluate the quality of machine translation or text generation tasks.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="BLEUEvaluator"/> computes the BLEU score of a response ("hypothesis") compared to one or more
/// reference responses supplied via <see cref="BLEUEvaluatorContext.References"/>. The score is returned in a
/// <see cref="NumericMetric"/> with a value between 0.0 and 1.0 where 0.0 represents no match at all and 1.0 indicates
/// a perfect match. By default, the score is interpreted with a pass/fail cutoff of 0.5. So a score of 0.5 or higher
/// is passing and a score below 0.5 is failing.
/// </para>
/// </remarks>
public sealed class BLEUEvaluator : IEvaluator
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="BLEUEvaluator"/>.
    /// </summary>
    public static string BLEUMetricName => "BLEU";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> EvaluationMetricNames { get; } = [BLEUMetricName];

    /// <inheritdoc/>
    public ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(modelResponse);

        var metric = new NumericMetric(BLEUMetricName);
        var result = new EvaluationResult(metric);
        metric.MarkAsBuiltIn();

        if (string.IsNullOrWhiteSpace(modelResponse.Text))
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error($"The {nameof(modelResponse)} supplied for evaluation was null or empty."));

            return new ValueTask<EvaluationResult>(result);
        }

        if (additionalContext?.OfType<BLEUEvaluatorContext>().FirstOrDefault()
                is not BLEUEvaluatorContext context)
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error(
                    $"A value of type '{nameof(BLEUEvaluatorContext)}' was not found in the '{nameof(additionalContext)}' collection."));

            return new ValueTask<EvaluationResult>(result);
        }

        if (context.References.Count is 0)
        {
            metric.AddDiagnostics(
                EvaluationDiagnostic.Error(
                    $"Supplied '{nameof(BLEUEvaluatorContext)}' did not contain any '{nameof(BLEUEvaluatorContext.References)}'."));

            return new ValueTask<EvaluationResult>(result);
        }

        (double score, TimeSpan duration) = TimingHelper.ExecuteWithTiming(() =>
        {
            string[][] references = context.References.Select(reference => SimpleWordTokenizer.WordTokenize(reference).ToArray()).ToArray();
            string[] hypothesis = SimpleWordTokenizer.WordTokenize(modelResponse.Text).ToArray();
            return BLEUAlgorithm.SentenceBLEU(references, hypothesis, BLEUAlgorithm.DefaultBLEUWeights, SmoothingFunction.Method4);
        });

        metric.Value = score;
        string durationText = $"{duration.TotalSeconds.ToString("F4", CultureInfo.InvariantCulture)} s";
        metric.AddOrUpdateMetadata(name: "evaluation-duration", value: durationText);
        metric.AddOrUpdateContext(context);
        metric.Interpretation = metric.Interpret();

        return new ValueTask<EvaluationResult>(result);
    }

}
