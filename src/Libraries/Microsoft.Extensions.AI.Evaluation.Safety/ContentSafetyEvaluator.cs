// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Utilities;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

/// <summary>
/// An <see langword="abstract"/> base class that can be used to implement <see cref="IEvaluator"/>s that utilize the
/// Azure AI Foundry Evaluation service to evaluate responses produced by an AI model for the presence of a variety of
/// unsafe content such as protected material, vulnerable code, harmful content etc.
/// </summary>
/// <param name="contentSafetyServiceAnnotationTask">
/// The name of the annotation task that should be used when communicating with the Azure AI Foundry Evaluation service
/// to perform evaluations.
/// </param>
/// <param name="metricNames">
/// A dictionary containing the mapping from the names of the metrics that are used when communicating with the Azure
/// AI Foundry Evaluation service, to the <see cref="EvaluationMetric.Name"/>s of the <see cref="EvaluationMetric"/>s
/// returned by this <see cref="IEvaluator"/>.
/// </param>
#pragma warning disable S1694 // An abstract class should have both abstract and concrete methods
public abstract class ContentSafetyEvaluator(
    string contentSafetyServiceAnnotationTask,
    IDictionary<string, string> metricNames) : IEvaluator
#pragma warning restore S1694
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> EvaluationMetricNames { get; } = [.. metricNames.Values];

    /// <inheritdoc/>
    public virtual ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatConfiguration);

        return EvaluateContentSafetyAsync(
            chatConfiguration.ChatClient,
            messages,
            modelResponse,
            additionalContext,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Evaluates the supplied <paramref name="modelResponse"/> using the Azure AI Foundry Evaluation Service and
    /// returns an <see cref="EvaluationResult"/> containing one or more <see cref="EvaluationMetric"/>s.
    /// </summary>
    /// <param name="contentSafetyServiceChatClient">
    /// The <see cref="IChatClient"/> that should be used to communicate with the Azure AI Foundry Evaluation Service
    /// when performing evaluations.
    /// </param>
    /// <param name="messages">
    /// The conversation history including the request that produced the supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="modelResponse">The response that is to be evaluated.</param>
    /// <param name="additionalContext">
    /// Additional contextual information (beyond that which is available in <paramref name="messages"/>) that the
    /// <see cref="IEvaluator"/> may need to accurately evaluate the supplied <paramref name="modelResponse"/>.
    /// </param>
    /// <param name="contentSafetyServicePayloadFormat">
    /// An identifier that specifies the format of the payload that should be used when communicating with the Azure AI
    /// Foundry Evaluation service to perform evaluations.
    /// </param>
    /// <param name="includeMetricNamesInContentSafetyServicePayload">
    /// A <see cref="bool"/> flag that indicates whether the names of the metrics should be included in the payload
    /// that is sent to the Azure AI Foundry Evaluation service when performing evaluations.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can cancel the evaluation operation.
    /// </param>
    /// <returns>An <see cref="EvaluationResult"/> containing one or more <see cref="EvaluationMetric"/>s.</returns>
    protected async ValueTask<EvaluationResult> EvaluateContentSafetyAsync(
        IChatClient contentSafetyServiceChatClient,
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        IEnumerable<EvaluationContext>? additionalContext = null,
        string contentSafetyServicePayloadFormat = "HumanSystem", // ContentSafetyServicePayloadFormat.HumanSystem.ToString()
        bool includeMetricNamesInContentSafetyServicePayload = true,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(contentSafetyServiceChatClient);
        _ = Throw.IfNull(modelResponse);

        ContentSafetyServicePayloadFormat payloadFormat =
#if NET
            Enum.Parse<ContentSafetyServicePayloadFormat>(contentSafetyServicePayloadFormat);
#else
            (ContentSafetyServicePayloadFormat)Enum.Parse(
                typeof(ContentSafetyServicePayloadFormat),
                contentSafetyServicePayloadFormat);
#endif

        IEnumerable<ChatMessage> conversation = [.. messages, .. modelResponse.Messages];

        string evaluatorName = GetType().Name;

        IEnumerable<string>? perTurnContext = null;
        if (additionalContext is not null && additionalContext.Any())
        {
            IReadOnlyList<EvaluationContext>? relevantContext = FilterAdditionalContext(additionalContext);

#pragma warning disable S1067 // Expressions should not be too complex
            if (relevantContext is not null && relevantContext.Any() &&
                relevantContext.SelectMany(c => c.Contents) is IEnumerable<AIContent> contents && contents.Any() &&
                contents.OfType<TextContent>() is IEnumerable<TextContent> textContents && textContents.Any() &&
                string.Join(Environment.NewLine, textContents.Select(c => c.Text)) is string contextString &&
                !string.IsNullOrWhiteSpace(contextString))
#pragma warning restore S1067
            {
                // Currently we only support supplying a context for the last conversation turn (which is the main one
                // that is being evaluated).
                perTurnContext = [contextString];
            }
        }

        (string payload, IReadOnlyList<EvaluationDiagnostic>? diagnostics) =
            ContentSafetyServicePayloadUtilities.GetPayload(
                payloadFormat,
                conversation,
                contentSafetyServiceAnnotationTask,
                evaluatorName,
                perTurnContext,
                metricNames: includeMetricNamesInContentSafetyServicePayload ? metricNames.Keys : null,
                cancellationToken);

        var payloadMessage = new ChatMessage(ChatRole.User, payload);

        (ChatResponse annotationResponse, TimeSpan annotationDuration) =
            await TimingHelper.ExecuteWithTimingAsync(() =>
                contentSafetyServiceChatClient.GetResponseAsync(
                    payloadMessage,
                    options: new ContentSafetyChatOptions(contentSafetyServiceAnnotationTask, evaluatorName),
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

        string annotationResult = annotationResponse.Text;
        EvaluationResult result = ContentSafetyService.ParseAnnotationResult(annotationResult);

        EvaluationResult updatedResult = UpdateMetrics();
        return updatedResult;

        EvaluationResult UpdateMetrics()
        {
            EvaluationResult updatedResult = new EvaluationResult();

            foreach (EvaluationMetric metric in result.Metrics.Values)
            {
                string contentSafetyServiceMetricName = metric.Name;
                if (metricNames.TryGetValue(contentSafetyServiceMetricName, out string? metricName))
                {
                    metric.Name = metricName;
                }

                metric.AddOrUpdateChatMetadata(annotationResponse, annotationDuration);

                metric.Interpretation =
                    metric switch
                    {
                        BooleanMetric booleanMetric => booleanMetric.InterpretContentSafetyScore(),
                        NumericMetric numericMetric => numericMetric.InterpretContentSafetyScore(),
                        _ => metric.Interpretation
                    };

                if (diagnostics is not null)
                {
                    metric.AddDiagnostics(diagnostics);
                }

#pragma warning disable S125 // Sections of code should not be commented out
                // The following commented code can be useful for debugging purposes.
                // metric.LogJsonData(payload);
                // metric.LogJsonData(annotationResult);
#pragma warning restore S125

                updatedResult.Metrics.Add(metric.Name, metric);
            }

            return updatedResult;
        }
    }

    /// <summary>
    /// Filters the <see cref="EvaluationContext"/>s supplied by the caller via <paramref name="additionalContext"/>
    /// down to just the <see cref="EvaluationContext"/>s that are relevant to the evaluation being performed by this
    /// <see cref="ContentSafetyEvaluator"/>.
    /// </summary>
    /// <param name="additionalContext">The <see cref="EvaluationContext"/>s supplied by the caller.</param>
    /// <returns>
    /// The <see cref="EvaluationContext"/>s that are relevant to the evaluation being performed by this
    /// <see cref="ContentSafetyEvaluator"/>.
    /// </returns>
    protected virtual IReadOnlyList<EvaluationContext>? FilterAdditionalContext(
        IEnumerable<EvaluationContext>? additionalContext)
            => null;
}
