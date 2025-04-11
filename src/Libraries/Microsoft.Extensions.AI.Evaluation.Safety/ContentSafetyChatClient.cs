// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal sealed class ContentSafetyChatClient : IChatClient
{
    private const string Moniker = "Azure AI Content Safety";

    private readonly ContentSafetyService _service;
    private readonly IChatClient? _originalChatClient;
    private readonly ChatClientMetadata _metadata;

    public ContentSafetyChatClient(
        ContentSafetyServiceConfiguration contentSafetyServiceConfiguration,
        IChatClient? originalChatClient = null)
    {
        _service = new ContentSafetyService(contentSafetyServiceConfiguration);
        _originalChatClient = originalChatClient;

        ChatClientMetadata? originalMetadata = _originalChatClient?.GetService<ChatClientMetadata>();

        string providerName =
            $"{Moniker} (" +
            $"Subscription: {contentSafetyServiceConfiguration.SubscriptionId}, " +
            $"Resource Group: {contentSafetyServiceConfiguration.ResourceGroupName}, " +
            $"Project: {contentSafetyServiceConfiguration.ProjectName})";

        if (originalMetadata?.ProviderName is string originalProviderName &&
            !string.IsNullOrWhiteSpace(originalProviderName))
        {
            providerName = $"{originalProviderName}; {providerName}";
        }

        string modelId = Moniker;

        if (originalMetadata?.DefaultModelId is string originalModelId &&
            !string.IsNullOrWhiteSpace(originalModelId))
        {
            modelId = $"{originalModelId}; {modelId}";
        }

        _metadata = new ChatClientMetadata(providerName, originalMetadata?.ProviderUri, modelId);
    }

    public async Task<ChatResponse> GetResponseAsync(
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

            return new ChatResponse(new ChatMessage(ChatRole.Assistant, annotationResult))
            {
                ModelId = Moniker
            };
        }
        else if (_originalChatClient is not null)
        {
            return await _originalChatClient.GetResponseAsync(
                messages,
                options,
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
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

            yield return new ChatResponseUpdate(ChatRole.Assistant, annotationResult)
            {
                ModelId = Moniker
            };
        }
        else if (_originalChatClient is not null)
        {
            await foreach (var update in
                _originalChatClient.GetStreamingResponseAsync(
                    messages,
                    options,
                    cancellationToken).ConfigureAwait(false))
            {
                yield return update;
            }
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceKey is null)
        {
            if (serviceType == typeof(ChatClientMetadata))
            {
                return _metadata;
            }
            else if (serviceType == typeof(ContentSafetyChatClient))
            {
                return this;
            }
        }

        return _originalChatClient?.GetService(serviceType, serviceKey);
    }

    public void Dispose()
        => _originalChatClient?.Dispose();
}
