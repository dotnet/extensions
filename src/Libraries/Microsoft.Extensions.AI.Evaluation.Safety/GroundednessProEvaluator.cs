// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

/// <summary>
/// An <see cref="IEvaluator"/> that utilizes the Azure AI Content Safety service to evaluate the groundedness of
/// responses produced by an AI model.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="GroundednessProEvaluator"/> measures the degree to which the response being evaluated is grounded in
/// the information present in the supplied <see cref="GroundednessProEvaluatorContext.GroundingContext"/>. It returns
/// a <see cref="NumericMetric"/> that contains a score for the groundedness. The score is a number between 1 and 5,
/// with 1 indicating a poor score, and 5 indicating an excellent score.
/// </para>
/// <para>
/// Note that <see cref="GroundednessProEvaluator"/> does not support evaluation of multimodal content present in the
/// evaluated responses. Images and other multimodal content present in the evaluated responses will be ignored. Also
/// note that if a multi-turn conversation is supplied as input, <see cref="GroundednessProEvaluator"/> will only
/// evaluate the contents of the last conversation turn. The contents of previous conversation turns will be ignored.
/// </para>
/// <para>
/// The Azure AI Content Safety service uses a finetuned model to perform this evaluation which is expected to
/// produce more accurate results than similar evaluations performed using a regular (non-finetuned) model.
/// </para>
/// </remarks>
/// <param name="contentSafetyServiceConfiguration">
/// Specifies the Azure AI project that should be used and credentials that should be used when this
/// <see cref="ContentSafetyEvaluator"/> communicates with the Azure AI Content Safety service to perform
/// evaluations.
/// </param>
public sealed class GroundednessProEvaluator(ContentSafetyServiceConfiguration contentSafetyServiceConfiguration)
    : ContentSafetyEvaluator(
        contentSafetyServiceConfiguration,
        contentSafetyServiceAnnotationTask: "groundedness",
        evaluatorName: nameof(GroundednessProEvaluator))
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="NumericMetric"/> returned by
    /// <see cref="GroundednessProEvaluator"/>.
    /// </summary>
    public static string GroundednessProMetricName => "Groundedness Pro";

    /// <inheritdoc/>
    public override IReadOnlyCollection<string> EvaluationMetricNames => [GroundednessProMetricName];

    /// <inheritdoc/>
    public override async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<string?> contexts;
        if (additionalContext?.OfType<GroundednessProEvaluatorContext>().FirstOrDefault()
                is GroundednessProEvaluatorContext context)
        {
            contexts = [context.GroundingContext];
        }
        else
        {
            throw new InvalidOperationException(
                $"A value of type '{nameof(GroundednessProEvaluatorContext)}' was not found in the '{nameof(additionalContext)}' collection.");
        }

        const string GenericGroundednessContentSafetyServiceMetricName = "generic_groundedness";

        EvaluationResult result =
            await EvaluateContentSafetyAsync(
                messages,
                modelResponse,
                contexts,
                contentSafetyServicePayloadFormat: ContentSafetyServicePayloadFormat.QuestionAnswer.ToString(),
                contentSafetyServiceMetricName: GenericGroundednessContentSafetyServiceMetricName,
                cancellationToken: cancellationToken).ConfigureAwait(false);

        IEnumerable<EvaluationMetric> updatedMetrics =
            result.Metrics.Values.Select(
                metric =>
                {
                    if (metric.Name == GenericGroundednessContentSafetyServiceMetricName)
                    {
                        metric.Name = GroundednessProMetricName;
                    }

                    return metric;
                });

        result = new EvaluationResult(updatedMetrics);
        result.Interpret(metric => metric is NumericMetric numericMetric ? numericMetric.InterpretScore() : null);
        return result;
    }
}
