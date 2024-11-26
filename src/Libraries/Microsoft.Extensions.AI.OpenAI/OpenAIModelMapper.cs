// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI.Chat;

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable S1135 // Track uses of "TODO" tags

namespace Microsoft.Extensions.AI;

internal static partial class OpenAIModelMappers
{
    private static readonly JsonElement _defaultParameterSchema = JsonDocument.Parse("{}").RootElement;

    internal static ChatCompletion FromOpenAIChatCompletion(OpenAI.Chat.ChatCompletion openAICompletion, ChatOptions? options)
    {
        _ = Throw.IfNull(openAICompletion);

        // Create the return message.
        ChatMessage returnMessage = new()
        {
            RawRepresentation = openAICompletion,
            Role = ToChatRole(openAICompletion.Role),
        };

        // Populate its content from those in the OpenAI response content.
        foreach (ChatMessageContentPart contentPart in openAICompletion.Content)
        {
            if (ToAIContent(contentPart) is AIContent aiContent)
            {
                returnMessage.Contents.Add(aiContent);
            }
        }

        // Also manufacture function calling content items from any tool calls in the response.
        if (options?.Tools is { Count: > 0 })
        {
            foreach (ChatToolCall toolCall in openAICompletion.ToolCalls)
            {
                if (!string.IsNullOrWhiteSpace(toolCall.FunctionName))
                {
                    var callContent = ParseCallContentFromBinaryData(toolCall.FunctionArguments, toolCall.Id, toolCall.FunctionName);
                    callContent.RawRepresentation = toolCall;

                    returnMessage.Contents.Add(callContent);
                }
            }
        }

        // Wrap the content in a ChatCompletion to return.
        var completion = new ChatCompletion([returnMessage])
        {
            RawRepresentation = openAICompletion,
            CompletionId = openAICompletion.Id,
            CreatedAt = openAICompletion.CreatedAt,
            ModelId = openAICompletion.Model,
            FinishReason = ToFinishReason(openAICompletion.FinishReason),
        };

        if (openAICompletion.Usage is ChatTokenUsage tokenUsage)
        {
            completion.Usage = ToUsageDetails(tokenUsage);
        }

        if (openAICompletion.ContentTokenLogProbabilities is { Count: > 0 } contentTokenLogProbs)
        {
            (completion.AdditionalProperties ??= [])[nameof(openAICompletion.ContentTokenLogProbabilities)] = contentTokenLogProbs;
        }

        if (openAICompletion.Refusal is string refusal)
        {
            (completion.AdditionalProperties ??= [])[nameof(openAICompletion.Refusal)] = refusal;
        }

        if (openAICompletion.RefusalTokenLogProbabilities is { Count: > 0 } refusalTokenLogProbs)
        {
            (completion.AdditionalProperties ??= [])[nameof(openAICompletion.RefusalTokenLogProbabilities)] = refusalTokenLogProbs;
        }

        if (openAICompletion.SystemFingerprint is string systemFingerprint)
        {
            (completion.AdditionalProperties ??= [])[nameof(openAICompletion.SystemFingerprint)] = systemFingerprint;
        }

        return completion;
    }

    internal static async IAsyncEnumerable<StreamingChatCompletionUpdate> FromOpenAIStreamingChatCompletionAsync(
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
            streamedRole ??= chatCompletionUpdate.Role is ChatMessageRole role ? ToChatRole(role) : null;
            finishReason ??= chatCompletionUpdate.FinishReason is OpenAI.Chat.ChatFinishReason reason ? ToFinishReason(reason) : null;
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
                var usageDetails = ToUsageDetails(tokenUsage);
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

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    internal static ChatCompletionOptions ToOpenAIOptions(ChatOptions? options)
    {
        ChatCompletionOptions result = new();

        if (options is not null)
        {
            result.FrequencyPenalty = options.FrequencyPenalty;
            result.MaxOutputTokenCount = options.MaxOutputTokens;
            result.TopP = options.TopP;
            result.PresencePenalty = options.PresencePenalty;
            result.Temperature = options.Temperature;
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
            result.Seed = options.Seed;
#pragma warning restore OPENAI001

            if (options.StopSequences is { Count: > 0 } stopSequences)
            {
                foreach (string stopSequence in stopSequences)
                {
                    result.StopSequences.Add(stopSequence);
                }
            }

            if (options.AdditionalProperties is { Count: > 0 } additionalProperties)
            {
                if (additionalProperties.TryGetValue(nameof(result.EndUserId), out string? endUserId))
                {
                    result.EndUserId = endUserId;
                }

                if (additionalProperties.TryGetValue(nameof(result.IncludeLogProbabilities), out bool includeLogProbabilities))
                {
                    result.IncludeLogProbabilities = includeLogProbabilities;
                }

                if (additionalProperties.TryGetValue(nameof(result.LogitBiases), out IDictionary<int, int>? logitBiases))
                {
                    foreach (KeyValuePair<int, int> kvp in logitBiases!)
                    {
                        result.LogitBiases[kvp.Key] = kvp.Value;
                    }
                }

                if (additionalProperties.TryGetValue(nameof(result.AllowParallelToolCalls), out bool allowParallelToolCalls))
                {
                    result.AllowParallelToolCalls = allowParallelToolCalls;
                }

                if (additionalProperties.TryGetValue(nameof(result.TopLogProbabilityCount), out int topLogProbabilityCountInt))
                {
                    result.TopLogProbabilityCount = topLogProbabilityCountInt;
                }
            }

            if (options.Tools is { Count: > 0 } tools)
            {
                foreach (AITool tool in tools)
                {
                    if (tool is AIFunction af)
                    {
                        result.Tools.Add(ToOpenAIChatTool(af));
                    }
                }

                switch (options.ToolMode)
                {
                    case AutoChatToolMode:
                        result.ToolChoice = ChatToolChoice.CreateAutoChoice();
                        break;

                    case RequiredChatToolMode required:
                        result.ToolChoice = required.RequiredFunctionName is null ?
                            ChatToolChoice.CreateRequiredChoice() :
                            ChatToolChoice.CreateFunctionChoice(required.RequiredFunctionName);
                        break;
                }
            }

            if (options.ResponseFormat is ChatResponseFormatText)
            {
                result.ResponseFormat = OpenAI.Chat.ChatResponseFormat.CreateTextFormat();
            }
            else if (options.ResponseFormat is ChatResponseFormatJson jsonFormat)
            {
                result.ResponseFormat = jsonFormat.Schema is string jsonSchema ?
                    OpenAI.Chat.ChatResponseFormat.CreateJsonSchemaFormat(jsonFormat.SchemaName ?? "json_schema", BinaryData.FromString(jsonSchema), jsonFormat.SchemaDescription) :
                    OpenAI.Chat.ChatResponseFormat.CreateJsonObjectFormat();
            }
        }

        return result;
    }

    /// <summary>Converts an Extensions function to an OpenAI chat tool.</summary>
    private static ChatTool ToOpenAIChatTool(AIFunction aiFunction)
    {
        bool? strict =
            aiFunction.Metadata.AdditionalProperties.TryGetValue("Strict", out object? strictObj) &&
            strictObj is bool strictValue ?
            strictValue : null;

        BinaryData resultParameters = OpenAIChatToolJson.ZeroFunctionParametersSchema;

        var parameters = aiFunction.Metadata.Parameters;
        if (parameters is { Count: > 0 })
        {
            OpenAIChatToolJson tool = new();

            foreach (AIFunctionParameterMetadata parameter in parameters)
            {
                tool.Properties.Add(parameter.Name, parameter.Schema is JsonElement e ? e : _defaultParameterSchema);

                if (parameter.IsRequired)
                {
                    tool.Required.Add(parameter.Name);
                }
            }

            resultParameters = BinaryData.FromBytes(
                JsonSerializer.SerializeToUtf8Bytes(tool, OpenAIJsonContext.Default.OpenAIChatToolJson));
        }

        return ChatTool.CreateFunctionTool(aiFunction.Metadata.Name, aiFunction.Metadata.Description, resultParameters, strict);
    }

    private static UsageDetails ToUsageDetails(ChatTokenUsage tokenUsage)
    {
        var destination = new UsageDetails
        {
            InputTokenCount = tokenUsage.InputTokenCount,
            OutputTokenCount = tokenUsage.OutputTokenCount,
            TotalTokenCount = tokenUsage.TotalTokenCount,
            AdditionalCounts = new(),
        };

        if (tokenUsage.InputTokenDetails is ChatInputTokenUsageDetails inputDetails)
        {
            if (inputDetails.AudioTokenCount is int audioTokenCount)
            {
                destination.AdditionalCounts.Add(
                    $"{nameof(ChatTokenUsage.InputTokenDetails)}.{nameof(ChatInputTokenUsageDetails.AudioTokenCount)}",
                    audioTokenCount);
            }

            if (inputDetails.CachedTokenCount is int cachedTokenCount)
            {
                destination.AdditionalCounts.Add(
                    $"{nameof(ChatTokenUsage.InputTokenDetails)}.{nameof(ChatInputTokenUsageDetails.CachedTokenCount)}",
                    cachedTokenCount);
            }
        }

        if (tokenUsage.OutputTokenDetails is ChatOutputTokenUsageDetails outputDetails)
        {
            if (outputDetails.AudioTokenCount is int audioTokenCount)
            {
                destination.AdditionalCounts.Add(
                    $"{nameof(ChatTokenUsage.OutputTokenDetails)}.{nameof(ChatOutputTokenUsageDetails.AudioTokenCount)}",
                    audioTokenCount);
            }

            destination.AdditionalCounts.Add(
                $"{nameof(ChatTokenUsage.OutputTokenDetails)}.{nameof(ChatOutputTokenUsageDetails.ReasoningTokenCount)}",
                outputDetails.ReasoningTokenCount);
        }

        return destination;
    }

    /// <summary>Converts an OpenAI role to an Extensions role.</summary>
    private static ChatRole ToChatRole(ChatMessageRole role) =>
        role switch
        {
            ChatMessageRole.System => ChatRole.System,
            ChatMessageRole.User => ChatRole.User,
            ChatMessageRole.Assistant => ChatRole.Assistant,
            ChatMessageRole.Tool => ChatRole.Tool,
            _ => new ChatRole(role.ToString()),
        };

    /// <summary>Creates an <see cref="AIContent"/> from a <see cref="ChatMessageContentPart"/>.</summary>
    /// <param name="contentPart">The content part to convert into a content.</param>
    /// <returns>The constructed <see cref="AIContent"/>, or null if the content part could not be converted.</returns>
    private static AIContent? ToAIContent(ChatMessageContentPart contentPart)
    {
        AIContent? aiContent = null;

        if (contentPart.Kind == ChatMessageContentPartKind.Text)
        {
            aiContent = new TextContent(contentPart.Text);
        }
        else if (contentPart.Kind == ChatMessageContentPartKind.Image)
        {
            ImageContent? imageContent;
            aiContent = imageContent =
                contentPart.ImageUri is not null ? new ImageContent(contentPart.ImageUri, contentPart.ImageBytesMediaType) :
                contentPart.ImageBytes is not null ? new ImageContent(contentPart.ImageBytes.ToMemory(), contentPart.ImageBytesMediaType) :
                null;

            if (imageContent is not null && contentPart.ImageDetailLevel?.ToString() is string detail)
            {
                (imageContent.AdditionalProperties ??= [])[nameof(contentPart.ImageDetailLevel)] = detail;
            }
        }

        if (aiContent is not null)
        {
            if (contentPart.Refusal is string refusal)
            {
                (aiContent.AdditionalProperties ??= [])[nameof(contentPart.Refusal)] = refusal;
            }

            aiContent.RawRepresentation = contentPart;
        }

        return aiContent;
    }

    /// <summary>Converts an OpenAI finish reason to an Extensions finish reason.</summary>
    private static ChatFinishReason? ToFinishReason(OpenAI.Chat.ChatFinishReason? finishReason) =>
        finishReason?.ToString() is not string s ? null :
        finishReason switch
        {
            OpenAI.Chat.ChatFinishReason.Stop => ChatFinishReason.Stop,
            OpenAI.Chat.ChatFinishReason.Length => ChatFinishReason.Length,
            OpenAI.Chat.ChatFinishReason.ContentFilter => ChatFinishReason.ContentFilter,
            OpenAI.Chat.ChatFinishReason.ToolCalls or OpenAI.Chat.ChatFinishReason.FunctionCall => ChatFinishReason.ToolCalls,
            _ => new ChatFinishReason(s),
        };

    private static FunctionCallContent ParseCallContentFromJsonString(string json, string callId, string name) =>
        FunctionCallContent.CreateFromParsedArguments(json, callId, name,
            argumentParser: static json => JsonSerializer.Deserialize(json, OpenAIJsonContext.Default.IDictionaryStringObject)!);

    private static FunctionCallContent ParseCallContentFromBinaryData(BinaryData ut8Json, string callId, string name) =>
        FunctionCallContent.CreateFromParsedArguments(ut8Json, callId, name,
            argumentParser: static json => JsonSerializer.Deserialize(json, OpenAIJsonContext.Default.IDictionaryStringObject)!);

    /// <summary>Used to create the JSON payload for an OpenAI chat tool description.</summary>
    public sealed class OpenAIChatToolJson
    {
        /// <summary>Gets a singleton JSON data for empty parameters. Optimization for the reasonably common case of a parameterless function.</summary>
        public static BinaryData ZeroFunctionParametersSchema { get; } = new("""{"type":"object","required":[],"properties":{}}"""u8.ToArray());

        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";

        [JsonPropertyName("required")]
        public List<string> Required { get; set; } = [];

        [JsonPropertyName("properties")]
        public Dictionary<string, JsonElement> Properties { get; set; } = [];
    }

    /// <summary>POCO representing function calling info. Used to concatenation information for a single function call from across multiple streaming updates.</summary>
    private sealed class FunctionCallInfo
    {
        public string? CallId;
        public string? Name;
        public StringBuilder? Arguments;
    }
}
