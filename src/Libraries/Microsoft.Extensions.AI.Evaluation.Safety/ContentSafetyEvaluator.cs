// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

/// <summary>
/// An <see langword="abstract"/> base class that can be used to implement <see cref="IEvaluator"/>s that utilize the
/// Azure AI Content Safety service to evaluate responses produced by an AI model for the presence of a variety of
/// unsafe content such as protected material, vulnerable code, harmful content etc.
/// </summary>
/// <param name="contentSafetyServiceConfiguration">
/// Specifies the Azure AI project that should be used and credentials that should be used when this
/// <see cref="ContentSafetyEvaluator"/> communicates with the Azure AI Content Safety service to perform evaluations.
/// </param>
/// <param name="contentSafetyServiceAnnotationTask">
/// The name of the annotation task that should be used when this <see cref="ContentSafetyEvaluator"/> communicates
/// with the Azure AI Content Safety service to perform evaluations.
/// </param>
/// <param name="evaluatorName">The name of the derived <see cref="ContentSafetyEvaluator"/>.</param>
public abstract class ContentSafetyEvaluator(
    ContentSafetyServiceConfiguration contentSafetyServiceConfiguration,
    string contentSafetyServiceAnnotationTask,
    string evaluatorName) : IEvaluator
{
    private readonly ContentSafetyService _service = new ContentSafetyService(contentSafetyServiceConfiguration);

    /// <inheritdoc/>
    public abstract IReadOnlyCollection<string> EvaluationMetricNames { get; }

    /// <inheritdoc/>
    public abstract ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates the supplied <paramref name="modelResponse"/> using the Azure AI Content Safety Service and returns
    /// an <see cref="EvaluationResult"/> containing one or more <see cref="EvaluationMetric"/>s.
    /// </summary>
    /// <param name="messages">
    /// The conversation history including the request that produced the supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="modelResponse">The response that is to be evaluated.</param>
    /// <param name="additionalContext">
    /// Per conversation turn contextual information (beyond that which is available in <paramref name="messages"/>)
    /// that the <see cref="IEvaluator"/> may need to accurately evaluate the supplied
    /// <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="contentSafetyServicePayloadFormat">
    /// An identifier that specifies the format of the payload that should be used when communicating with the Azure AI
    /// Content Safety service to perform evaluations.
    /// </param>
    /// <param name="contentSafetyServiceMetricName">
    /// The name of the metric that should be used in the payload when communicating with the Azure AI Content Safety
    /// service to perform evaluations.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can cancel the evaluation operation.
    /// </param>
    /// <returns>An <see cref="EvaluationResult"/> containing one or more <see cref="EvaluationMetric"/>s.</returns>
    protected ValueTask<EvaluationResult> EvaluateContentSafetyAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        IEnumerable<string?>? additionalContext = null,
        string contentSafetyServicePayloadFormat = "HumanSystem", // ContentSafetyServicePayloadFormat.HumanSystem.ToString()
        string? contentSafetyServiceMetricName = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(modelResponse);

        ContentSafetyServicePayloadFormat payloadFormat =
#if NET
            Enum.Parse<ContentSafetyServicePayloadFormat>(contentSafetyServicePayloadFormat);
#else
            (ContentSafetyServicePayloadFormat)Enum.Parse(
                typeof(ContentSafetyServicePayloadFormat),
                contentSafetyServicePayloadFormat);
#endif

        return _service.EvaluateAsync(
            [.. messages, .. modelResponse.Messages],
            contentSafetyServiceAnnotationTask,
            evaluatorName,
            additionalContext,
            payloadFormat,
            metricNames: string.IsNullOrWhiteSpace(contentSafetyServiceMetricName) ? null : [contentSafetyServiceMetricName!],
            cancellationToken);
    }
}
