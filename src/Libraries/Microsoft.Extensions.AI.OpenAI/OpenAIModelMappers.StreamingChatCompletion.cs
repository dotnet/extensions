// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable S103 // Lines should not be too long
#pragma warning disable CA1859 // Use concrete types when possible for improved performance

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenAI.Chat;

namespace Microsoft.Extensions.AI;

internal static partial class OpenAIModelMappers
{
    public static async IAsyncEnumerable<OpenAI.Chat.StreamingChatCompletionUpdate> ToOpenAIStreamingChatCompletionAsync(
        IAsyncEnumerable<StreamingChatCompletionUpdate> chatCompletions,
        JsonSerializerOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chatCompletionUpdate in chatCompletions.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            List<StreamingChatToolCallUpdate>? toolCallUpdates = null;
            ChatTokenUsage? chatTokenUsage = null;

            foreach (var content in chatCompletionUpdate.Contents)
            {
                if (content is FunctionCallContent functionCallContent)
                {
                    toolCallUpdates ??= [];
                    toolCallUpdates.Add(OpenAIChatModelFactory.StreamingChatToolCallUpdate(
                        index: toolCallUpdates.Count,
                        toolCallId: functionCallContent.CallId,
                        functionName: functionCallContent.Name,
                        functionArgumentsUpdate: new(JsonSerializer.SerializeToUtf8Bytes(functionCallContent.Arguments, options.GetTypeInfo(typeof(IDictionary<string, object?>))))));
                }
                else if (content is UsageContent usageContent)
                {
                    chatTokenUsage = ToOpenAIUsage(usageContent.Details);
                }
            }

            yield return OpenAIChatModelFactory.StreamingChatCompletionUpdate(
                completionId: chatCompletionUpdate.CompletionId,
                model: chatCompletionUpdate.ModelId,
                createdAt: chatCompletionUpdate.CreatedAt ?? default,
                role: ToOpenAIChatRole(chatCompletionUpdate.Role),
                finishReason: ToOpenAIFinishReason(chatCompletionUpdate.FinishReason),
                contentUpdate: [.. ToOpenAIChatContent(chatCompletionUpdate.Contents)],
                toolCallUpdates: toolCallUpdates,
                refusalUpdate: chatCompletionUpdate.AdditionalProperties.GetValueOrDefault<string>(nameof(OpenAI.Chat.StreamingChatCompletionUpdate.RefusalUpdate)),
                contentTokenLogProbabilities: chatCompletionUpdate.AdditionalProperties.GetValueOrDefault<IReadOnlyList<ChatTokenLogProbabilityDetails>>(nameof(OpenAI.Chat.StreamingChatCompletionUpdate.ContentTokenLogProbabilities)),
                refusalTokenLogProbabilities: chatCompletionUpdate.AdditionalProperties.GetValueOrDefault<IReadOnlyList<ChatTokenLogProbabilityDetails>>(nameof(OpenAI.Chat.StreamingChatCompletionUpdate.RefusalTokenLogProbabilities)),
                systemFingerprint: chatCompletionUpdate.AdditionalProperties.GetValueOrDefault<string>(nameof(OpenAI.Chat.StreamingChatCompletionUpdate.SystemFingerprint)),
                usage: chatTokenUsage);
        }
    }

    public static async IAsyncEnumerable<StreamingChatCompletionUpdate> FromOpenAIStreamingChatCompletionAsync(
        IAsyncEnumerable<OpenAI.Chat.StreamingChatCompletionUpdate> chatCompletionUpdates,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Dictionary<int, FunctionCallInfo>? functionCallInfos = null;
        ChatRole? streamedRole = null;
        ChatFinishReason? finishReason = null;
        StringBuilder? refusal = null;
        string? completionId = null;
        DateTimeOffset? createdAt = null;
        string? modelId = null;
        string? fingerprint = null;

        // Process each update as it arrives
        await foreach (OpenAI.Chat.StreamingChatCompletionUpdate chatCompletionUpdate in chatCompletionUpdates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // The role and finish reason may arrive during any update, but once they've arrived, the same value should be the same for all subsequent updates.
            streamedRole ??= chatCompletionUpdate.Role is ChatMessageRole role ? FromOpenAIChatRole(role) : null;
            finishReason ??= chatCompletionUpdate.FinishReason is OpenAI.Chat.ChatFinishReason reason ? FromOpenAIFinishReason(reason) : null;
            completionId ??= chatCompletionUpdate.CompletionId;
            createdAt ??= chatCompletionUpdate.CreatedAt;
            modelId ??= chatCompletionUpdate.Model;
            fingerprint ??= chatCompletionUpdate.SystemFingerprint;

            // Create the response content object.
            StreamingChatCompletionUpdate completionUpdate = new()
            {
                CompletionId = chatCompletionUpdate.CompletionId,
                CreatedAt = chatCompletionUpdate.CreatedAt,
                FinishReason = finishReason,
                ModelId = modelId,
                RawRepresentation = chatCompletionUpdate,
                Role = streamedRole,
            };

            // Populate it with any additional metadata from the OpenAI object.
            if (chatCompletionUpdate.ContentTokenLogProbabilities is { Count: > 0 } contentTokenLogProbs)
            {
                (completionUpdate.AdditionalProperties ??= [])[nameof(chatCompletionUpdate.ContentTokenLogProbabilities)] = contentTokenLogProbs;
            }

            if (chatCompletionUpdate.RefusalTokenLogProbabilities is { Count: > 0 } refusalTokenLogProbs)
            {
                (completionUpdate.AdditionalProperties ??= [])[nameof(chatCompletionUpdate.RefusalTokenLogProbabilities)] = refusalTokenLogProbs;
            }

            if (fingerprint is not null)
            {
                (completionUpdate.AdditionalProperties ??= [])[nameof(chatCompletionUpdate.SystemFingerprint)] = fingerprint;
            }

            // Transfer over content update items.
            if (chatCompletionUpdate.ContentUpdate is { Count: > 0 })
            {
                foreach (ChatMessageContentPart contentPart in chatCompletionUpdate.ContentUpdate)
                {
                    if (ToAIContent(contentPart) is AIContent aiContent)
                    {
                        completionUpdate.Contents.Add(aiContent);
                    }
                }
            }

            // Transfer over refusal updates.
            if (chatCompletionUpdate.RefusalUpdate is not null)
            {
                _ = (refusal ??= new()).Append(chatCompletionUpdate.RefusalUpdate);
            }

            // Transfer over tool call updates.
            if (chatCompletionUpdate.ToolCallUpdates is { Count: > 0 } toolCallUpdates)
            {
                foreach (StreamingChatToolCallUpdate toolCallUpdate in toolCallUpdates)
                {
                    functionCallInfos ??= [];
                    if (!functionCallInfos.TryGetValue(toolCallUpdate.Index, out FunctionCallInfo? existing))
                    {
                        functionCallInfos[toolCallUpdate.Index] = existing = new();
                    }

                    existing.CallId ??= toolCallUpdate.ToolCallId;
                    existing.Name ??= toolCallUpdate.FunctionName;
                    if (toolCallUpdate.FunctionArgumentsUpdate is { } update && !update.ToMemory().IsEmpty)
                    {
                        _ = (existing.Arguments ??= new()).Append(update.ToString());
                    }
                }
            }

            // Transfer over usage updates.
            if (chatCompletionUpdate.Usage is ChatTokenUsage tokenUsage)
            {
                var usageDetails = FromOpenAIUsage(tokenUsage);
                completionUpdate.Contents.Add(new UsageContent(usageDetails));
            }

            // Now yield the item.
            yield return completionUpdate;
        }

        // Now that we've received all updates, combine any for function calls into a single item to yield.
        if (functionCallInfos is not null)
        {
            StreamingChatCompletionUpdate completionUpdate = new()
            {
                CompletionId = completionId,
                CreatedAt = createdAt,
                FinishReason = finishReason,
                ModelId = modelId,
                Role = streamedRole,
            };

            foreach (var entry in functionCallInfos)
            {
                FunctionCallInfo fci = entry.Value;
                if (!string.IsNullOrWhiteSpace(fci.Name))
                {
                    var callContent = ParseCallContentFromJsonString(
                        fci.Arguments?.ToString() ?? string.Empty,
                        fci.CallId!,
                        fci.Name!);
                    completionUpdate.Contents.Add(callContent);
                }
            }

            // Refusals are about the model not following the schema for tool calls. As such, if we have any refusal,
            // add it to this function calling item.
            if (refusal is not null)
            {
                (completionUpdate.AdditionalProperties ??= [])[nameof(ChatMessageContentPart.Refusal)] = refusal.ToString();
            }

            // Propagate additional relevant metadata.
            if (fingerprint is not null)
            {
                (completionUpdate.AdditionalProperties ??= [])[nameof(OpenAI.Chat.ChatCompletion.SystemFingerprint)] = fingerprint;
            }

            yield return completionUpdate;
        }
    }
}
