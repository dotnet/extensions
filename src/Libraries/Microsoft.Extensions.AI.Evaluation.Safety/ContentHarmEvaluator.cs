// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

#pragma warning disable S1694 // An abstract class should have both abstract and concrete methods
/// <summary>
/// An <see langword="abstract"/> base class that can be used to implement <see cref="IEvaluator"/>s that utilize the
/// Azure AI Content Safety service to evaluate responses produced by an AI model for the presence of a variety of
/// harmful content such as violence, hate speech, etc.
/// </summary>
/// <param name="contentSafetyServiceConfiguration">
/// Specifies the Azure AI project that should be used and credentials that should be used when this
/// <see cref="ContentSafetyEvaluator"/> communicates with the Azure AI Content Safety service to perform evaluations.
/// </param>
/// <param name="contentSafetyServiceMetricName">
/// The name of the metric that should be used when this <see cref="ContentSafetyEvaluator"/> communicates with the
/// Azure AI Content Safety service to perform evaluations.
/// </param>
/// <param name="metricName">
/// The name of the <see cref="EvaluationMetric"/> produced by this <see cref="ContentSafetyEvaluator"/>.
/// </param>
/// <param name="evaluatorName">The name of the derived <see cref="ContentSafetyEvaluator"/>.</param>
public abstract class ContentHarmEvaluator(
    ContentSafetyServiceConfiguration contentSafetyServiceConfiguration,
    string contentSafetyServiceMetricName,
    string metricName,
    string evaluatorName)
        : ContentSafetyEvaluator(
            contentSafetyServiceConfiguration,
            contentSafetyServiceAnnotationTask: "content harm",
            evaluatorName)
#pragma warning restore S1694
{
    /// <inheritdoc/>
    public override IReadOnlyCollection<string> EvaluationMetricNames => [metricName];

    /// <inheritdoc/>
    public sealed override async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        EvaluationResult result =
            await EvaluateContentSafetyAsync(
                messages,
                modelResponse,
                contentSafetyServicePayloadFormat: ContentSafetyServicePayloadFormat.Conversation.ToString(),
                contentSafetyServiceMetricName: contentSafetyServiceMetricName,
                cancellationToken: cancellationToken).ConfigureAwait(false);

        IEnumerable<EvaluationMetric> updatedMetrics =
            result.Metrics.Values.Select(
                metric =>
                {
                    if (metric.Name == contentSafetyServiceMetricName)
                    {
                        metric.Name = metricName;
                    }

                    return metric;
                });

        result = new EvaluationResult(updatedMetrics);
        result.Interpret(metric => metric is NumericMetric numericMetric ? numericMetric.InterpretHarmScore() : null);
        return result;
    }
}
