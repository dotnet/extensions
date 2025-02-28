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
    public static async IAsyncEnumerable<StreamingChatCompletionUpdate> ToOpenAIStreamingChatCompletionAsync(
        IAsyncEnumerable<ChatResponseUpdate> updates,
        JsonSerializerOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            List<StreamingChatToolCallUpdate>? toolCallUpdates = null;
            ChatTokenUsage? chatTokenUsage = null;

            foreach (var content in update.Contents)
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
                completionId: update.ResponseId ?? CreateCompletionId(),
                model: update.ModelId,
                createdAt: update.CreatedAt ?? DateTimeOffset.UtcNow,
                role: ToOpenAIChatRole(update.Role),
                finishReason: update.FinishReason is null ? null : ToOpenAIFinishReason(update.FinishReason),
                contentUpdate: [.. ToOpenAIChatContent(update.Contents)],
                toolCallUpdates: toolCallUpdates,
                refusalUpdate: update.AdditionalProperties.GetValueOrDefault<string>(nameof(StreamingChatCompletionUpdate.RefusalUpdate)),
                contentTokenLogProbabilities: update.AdditionalProperties.GetValueOrDefault<IReadOnlyList<ChatTokenLogProbabilityDetails>>(nameof(StreamingChatCompletionUpdate.ContentTokenLogProbabilities)),
                refusalTokenLogProbabilities: update.AdditionalProperties.GetValueOrDefault<IReadOnlyList<ChatTokenLogProbabilityDetails>>(nameof(StreamingChatCompletionUpdate.RefusalTokenLogProbabilities)),
                systemFingerprint: update.AdditionalProperties.GetValueOrDefault<string>(nameof(StreamingChatCompletionUpdate.SystemFingerprint)),
                usage: chatTokenUsage);
        }
    }

    public static async IAsyncEnumerable<ChatResponseUpdate> FromOpenAIStreamingChatCompletionAsync(
        IAsyncEnumerable<StreamingChatCompletionUpdate> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Dictionary<int, FunctionCallInfo>? functionCallInfos = null;
        ChatRole? streamedRole = null;
        ChatFinishReason? finishReason = null;
        StringBuilder? refusal = null;
        string? responseId = null;
        DateTimeOffset? createdAt = null;
        string? modelId = null;
        string? fingerprint = null;

        // Process each update as it arrives
        await foreach (StreamingChatCompletionUpdate update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // The role and finish reason may arrive during any update, but once they've arrived, the same value should be the same for all subsequent updates.
            streamedRole ??= update.Role is ChatMessageRole role ? FromOpenAIChatRole(role) : null;
            finishReason ??= update.FinishReason is OpenAI.Chat.ChatFinishReason reason ? FromOpenAIFinishReason(reason) : null;
            responseId ??= update.CompletionId;
            createdAt ??= update.CreatedAt;
            modelId ??= update.Model;
            fingerprint ??= update.SystemFingerprint;

            // Create the response content object.
            ChatResponseUpdate responseUpdate = new()
            {
                ResponseId = update.CompletionId,
                CreatedAt = update.CreatedAt,
                FinishReason = finishReason,
                ModelId = modelId,
                RawRepresentation = update,
                Role = streamedRole,
            };

            // Populate it with any additional metadata from the OpenAI object.
            if (update.ContentTokenLogProbabilities is { Count: > 0 } contentTokenLogProbs)
            {
                (responseUpdate.AdditionalProperties ??= [])[nameof(update.ContentTokenLogProbabilities)] = contentTokenLogProbs;
            }

            if (update.RefusalTokenLogProbabilities is { Count: > 0 } refusalTokenLogProbs)
            {
                (responseUpdate.AdditionalProperties ??= [])[nameof(update.RefusalTokenLogProbabilities)] = refusalTokenLogProbs;
            }

            if (fingerprint is not null)
            {
                (responseUpdate.AdditionalProperties ??= [])[nameof(update.SystemFingerprint)] = fingerprint;
            }

            // Transfer over content update items.
            if (update.ContentUpdate is { Count: > 0 })
            {
                foreach (ChatMessageContentPart contentPart in update.ContentUpdate)
                {
                    if (ToAIContent(contentPart) is AIContent aiContent)
                    {
                        responseUpdate.Contents.Add(aiContent);
                    }
                }
            }

            // Transfer over refusal updates.
            if (update.RefusalUpdate is not null)
            {
                _ = (refusal ??= new()).Append(update.RefusalUpdate);
            }

            // Transfer over tool call updates.
            if (update.ToolCallUpdates is { Count: > 0 } toolCallUpdates)
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
                    if (toolCallUpdate.FunctionArgumentsUpdate is { } argUpdate && !argUpdate.ToMemory().IsEmpty)
                    {
                        _ = (existing.Arguments ??= new()).Append(argUpdate.ToString());
                    }
                }
            }

            // Transfer over usage updates.
            if (update.Usage is ChatTokenUsage tokenUsage)
            {
                var usageDetails = FromOpenAIUsage(tokenUsage);
                responseUpdate.Contents.Add(new UsageContent(usageDetails));
            }

            // Now yield the item.
            yield return responseUpdate;
        }

        // Now that we've received all updates, combine any for function calls into a single item to yield.
        if (functionCallInfos is not null)
        {
            ChatResponseUpdate responseUpdate = new()
            {
                ResponseId = responseId,
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
                    responseUpdate.Contents.Add(callContent);
                }
            }

            // Refusals are about the model not following the schema for tool calls. As such, if we have any refusal,
            // add it to this function calling item.
            if (refusal is not null)
            {
                (responseUpdate.AdditionalProperties ??= [])[nameof(ChatMessageContentPart.Refusal)] = refusal.ToString();
            }

            // Propagate additional relevant metadata.
            if (fingerprint is not null)
            {
                (responseUpdate.AdditionalProperties ??= [])[nameof(ChatCompletion.SystemFingerprint)] = fingerprint;
            }

            yield return responseUpdate;
        }
    }
}
