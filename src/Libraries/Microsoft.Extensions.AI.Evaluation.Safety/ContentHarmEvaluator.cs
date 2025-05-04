// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

/// <summary>
/// An <see cref="IEvaluator"/> that utilizes the Azure AI Foundry Evaluation service to evaluate responses produced by
/// an AI model for the presence of a variety of harmful content such as violence, hate speech, etc.
/// </summary>
/// <remarks>
/// <see cref="ContentHarmEvaluator"/> can be used to evaluate responses for all supported content harm metrics in one
/// go. You can achieve this by omitting the <paramref name="metricNames"/> parameter.
///
/// <see cref="ContentHarmEvaluator"/> also serves as a base class for <see cref="HateAndUnfairnessEvaluator"/>,
/// <see cref="ViolenceEvaluator"/>, <see cref="SelfHarmEvaluator"/> and <see cref="SexualEvaluator"/> which can be
/// used to evaluate responses for one specific content harm metric at a time.
/// </remarks>
/// <param name="metricNames">
/// A optional dictionary containing the mapping from the names of the metrics that are used when communicating
/// with the Azure AI Foundry Evaluation service, to the <see cref="EvaluationMetric.Name"/>s of the
/// <see cref="EvaluationMetric"/>s returned by this <see cref="IEvaluator"/>.
///
/// If omitted, includes mappings for all content harm metrics that are supported by the Azure AI Foundry Evaluation
/// service. This includes <see cref="HateAndUnfairnessEvaluator.HateAndUnfairnessMetricName"/>,
/// <see cref="ViolenceEvaluator.ViolenceMetricName"/>, <see cref="SelfHarmEvaluator.SelfHarmMetricName"/> and
/// <see cref="SexualEvaluator.SexualMetricName"/>.
/// </param>
#pragma warning disable SA1118 // Parameter should not span multiple lines
public class ContentHarmEvaluator(IDictionary<string, string>? metricNames = null)
    : ContentSafetyEvaluator(
        contentSafetyServiceAnnotationTask: "content harm",
        metricNames ??
            new Dictionary<string, string>
            {
                ["hate_fairness"] = HateAndUnfairnessEvaluator.HateAndUnfairnessMetricName,
                ["violence"] = ViolenceEvaluator.ViolenceMetricName,
                ["self_harm"] = SelfHarmEvaluator.SelfHarmMetricName,
                ["sexual"] = SexualEvaluator.SexualMetricName
            })
#pragma warning restore SA1118
{
    /// <inheritdoc/>
    public sealed override async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatConfiguration);
        _ = Throw.IfNull(modelResponse);

        EvaluationResult result =
            await EvaluateContentSafetyAsync(
                chatConfiguration.ChatClient,
                messages,
                modelResponse,
                additionalContext,
                contentSafetyServicePayloadFormat: ContentSafetyServicePayloadFormat.Conversation.ToString(),
                cancellationToken: cancellationToken).ConfigureAwait(false);

        result.Interpret(
            metric => metric is NumericMetric numericMetric ? numericMetric.InterpretContentHarmScore() : null);

        return result;
    }
}
