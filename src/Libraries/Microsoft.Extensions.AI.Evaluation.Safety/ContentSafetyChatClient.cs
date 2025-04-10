// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal sealed partial class ContentSafetyChatClient(
    ContentSafetyServiceConfiguration contentSafetyServiceConfiguration,
    IChatClient? originalChatClient = null) : DelegatingChatClient(originalChatClient ?? NoOpChatClient.Instance)
{
    private readonly ContentSafetyService _service = new ContentSafetyService(contentSafetyServiceConfiguration);

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (options is ContentSafetyChatOptions contentSafetyChatOptions)
        {
            Debug.Assert(messages.Any() && !messages.Skip(1).Any(), $"Expected exactly one message.");
            string payload = messages.Single().Text;

            string annotationResult =
                await _service.AnnotateAsync(
                    payload,
                    contentSafetyChatOptions.AnnotationTask,
                    contentSafetyChatOptions.EvaluatorName,
                    cancellationToken).ConfigureAwait(false);

            return new ChatResponse(new ChatMessage(ChatRole.Assistant, annotationResult));
        }
        else
        {
            return await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (options is ContentSafetyChatOptions contentSafetyChatOptions)
        {
            Debug.Assert(messages.Any() && !messages.Skip(1).Any(), $"Expected exactly one message.");
            string payload = messages.Single().Text;

            string annotationResult =
                await _service.AnnotateAsync(
                    payload,
                    contentSafetyChatOptions.AnnotationTask,
                    contentSafetyChatOptions.EvaluatorName,
                    cancellationToken).ConfigureAwait(false);

            yield return new ChatResponseUpdate(ChatRole.Assistant, annotationResult);
        }
        else
        {
            await foreach (var update in
                base.GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
            {
                yield return update;
            }
        }
    }
}
