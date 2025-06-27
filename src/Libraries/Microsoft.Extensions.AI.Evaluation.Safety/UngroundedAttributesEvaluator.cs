// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

/// <summary>
/// An <see cref="IEvaluator"/> that utilizes the Azure AI Foundry Evaluation service to evaluate responses produced by
/// an AI model for presence of content that indicates ungrounded inference of human attributes.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="UngroundedAttributesEvaluator"/> checks whether the response being evaluated is first, ungrounded
/// based on the information present in the supplied
/// <see cref="UngroundedAttributesEvaluatorContext.GroundingContext"/>. It then checks whether the response contains
/// information about the protected class or emotional state of a person. It returns a <see cref="BooleanMetric"/>
/// with a value of <see langword="false"/> indicating an excellent score, and a value of <see langword="true"/>
/// indicating a poor score.
/// </para>
/// <para>
/// Note that <see cref="UngroundedAttributesEvaluator"/> does not support evaluation of multimodal content present in
/// the evaluated responses. Images and other multimodal content present in the evaluated responses will be ignored.
/// Also note that if a multi-turn conversation is supplied as input, <see cref="UngroundedAttributesEvaluator"/> will
/// only evaluate the contents of the last conversation turn. The contents of previous conversation turns will be
/// ignored.
/// </para>
/// <para>
/// The Azure AI Foundry Evaluation service uses a finetuned model to perform this evaluation which is expected to
/// produce more accurate results than similar evaluations performed using a regular (non-finetuned) model.
/// </para>
/// </remarks>
public sealed class UngroundedAttributesEvaluator()
    : ContentSafetyEvaluator(
        contentSafetyServiceAnnotationTask: "inference sensitive attributes",
        metricNames:
            new Dictionary<string, string> { ["inference_sensitive_attributes"] = UngroundedAttributesMetricName })
{
    /// <summary>
    /// Gets the <see cref="EvaluationMetric.Name"/> of the <see cref="BooleanMetric"/> returned by
    /// <see cref="UngroundedAttributesEvaluator"/>.
    /// </summary>
    public static string UngroundedAttributesMetricName => "Ungrounded Attributes";

    /// <inheritdoc/>
    public override async ValueTask<EvaluationResult> EvaluateAsync(
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
                contentSafetyServicePayloadFormat: ContentSafetyServicePayloadFormat.QueryResponse.ToString(),
                cancellationToken: cancellationToken).ConfigureAwait(false);

        UngroundedAttributesEvaluatorContext context = GetRelevantContext(additionalContext);
        result.AddOrUpdateContextInAllMetrics(context);

        return result;
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<EvaluationContext>? FilterAdditionalContext(
        IEnumerable<EvaluationContext>? additionalContext)
    {
        UngroundedAttributesEvaluatorContext context = GetRelevantContext(additionalContext);
        return [context];
    }

    private static UngroundedAttributesEvaluatorContext GetRelevantContext(
        IEnumerable<EvaluationContext>? additionalContext)
    {
        if (additionalContext?.OfType<UngroundedAttributesEvaluatorContext>().FirstOrDefault()
                is UngroundedAttributesEvaluatorContext context)
        {
            return context;
        }

        throw new InvalidOperationException(
            $"A value of type {nameof(UngroundedAttributesEvaluatorContext)} was not found in the {nameof(additionalContext)} collection.");
    }
}
