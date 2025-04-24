// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

/// <summary>
/// An <see langword="abstract"/> base class that can be used to implement <see cref="IEvaluator"/>s that utilize the
/// Azure AI Content Safety service to evaluate responses produced by an AI model for the presence of a variety of
/// harmful content such as violence, hate speech, etc.
/// </summary>
/// <param name="metricNames">
/// A dictionary containing the mapping from the names of the metrics that are used when communicating with the Azure
/// AI Content Safety to the <see cref="EvaluationMetric.Name"/>s of the <see cref="EvaluationMetric"/>s returned by
/// this <see cref="IEvaluator"/>.
/// </param>
#pragma warning disable S1694 // An abstract class should have both abstract and concrete methods
public abstract class ContentHarmEvaluator(IDictionary<string, string> metricNames)
    : ContentSafetyEvaluator(contentSafetyServiceAnnotationTask: "content harm", metricNames)
#pragma warning restore S1694
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
