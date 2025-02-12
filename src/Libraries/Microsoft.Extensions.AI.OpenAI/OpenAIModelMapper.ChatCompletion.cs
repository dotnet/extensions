// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI.Chat;

#pragma warning disable CA1308 // Normalize strings to uppercase
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable S103 // Lines should not be too long
#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S2178 // Short-circuit logic should be used in boolean contexts
#pragma warning disable S3440 // Variables should not be checked against the values they're about to be assigned
#pragma warning disable EA0011 // Consider removing unnecessary conditional access operator (?)

namespace Microsoft.Extensions.AI;

internal static partial class OpenAIModelMappers
{
    internal static JsonElement DefaultParameterSchema { get; } = JsonDocument.Parse("{}").RootElement;

    public static ChatCompletion ToOpenAIChatCompletion(ChatResponse response, JsonSerializerOptions options)
    {
        _ = Throw.IfNull(response);

        if (response.Choices.Count > 1)
        {
            throw new NotSupportedException("Creating OpenAI ChatCompletion models with multiple choices is currently not supported.");
        }

        List<ChatToolCall>? toolCalls = null;
        foreach (AIContent content in response.Message.Contents)
        {
            if (content is FunctionCallContent callRequest)
            {
                toolCalls ??= [];
                toolCalls.Add(ChatToolCall.CreateFunctionToolCall(
                    callRequest.CallId,
                    callRequest.Name,
                    new(JsonSerializer.SerializeToUtf8Bytes(
                        callRequest.Arguments,
                        options.GetTypeInfo(typeof(IDictionary<string, object?>))))));
            }
        }

        ChatTokenUsage? chatTokenUsage = null;
        if (response.Usage is UsageDetails usageDetails)
        {
            chatTokenUsage = ToOpenAIUsage(usageDetails);
        }

        return OpenAIChatModelFactory.ChatCompletion(
            id: response.ResponseId ?? CreateCompletionId(),
            model: response.ModelId,
            createdAt: response.CreatedAt ?? DateTimeOffset.UtcNow,
            role: ToOpenAIChatRole(response.Message.Role).Value,
            finishReason: ToOpenAIFinishReason(response.FinishReason),
            content: new(ToOpenAIChatContent(response.Message.Contents)),
            toolCalls: toolCalls,
            refusal: response.AdditionalProperties.GetValueOrDefault<string>(nameof(ChatCompletion.Refusal)),
            contentTokenLogProbabilities: response.AdditionalProperties.GetValueOrDefault<IReadOnlyList<ChatTokenLogProbabilityDetails>>(nameof(ChatCompletion.ContentTokenLogProbabilities)),
            refusalTokenLogProbabilities: response.AdditionalProperties.GetValueOrDefault<IReadOnlyList<ChatTokenLogProbabilityDetails>>(nameof(ChatCompletion.RefusalTokenLogProbabilities)),
            systemFingerprint: response.AdditionalProperties.GetValueOrDefault<string>(nameof(ChatCompletion.SystemFingerprint)),
            usage: chatTokenUsage);
    }

    public static ChatResponse FromOpenAIChatCompletion(ChatCompletion openAICompletion, ChatOptions? options, ChatCompletionOptions chatCompletionOptions)
    {
        _ = Throw.IfNull(openAICompletion);

        // Create the return message.
        ChatMessage returnMessage = new()
        {
            RawRepresentation = openAICompletion,
            Role = FromOpenAIChatRole(openAICompletion.Role),
        };

        // Populate its content from those in the OpenAI response content.
        foreach (ChatMessageContentPart contentPart in openAICompletion.Content)
        {
            if (ToAIContent(contentPart) is AIContent aiContent)
            {
                returnMessage.Contents.Add(aiContent);
            }
        }

        // Output audio is handled separately from message content parts.
        if (openAICompletion.OutputAudio is ChatOutputAudio audio)
        {
            string mimeType = chatCompletionOptions?.AudioOptions?.OutputAudioFormat.ToString()?.ToLowerInvariant() switch
            {
                "opus" => "audio/opus",
                "aac" => "audio/aac",
                "flac" => "audio/flac",
                "wav" => "audio/wav",
                "pcm" => "audio/pcm",
                "mp3" or _ => "audio/mpeg",
            };

            var dc = new DataContent(audio.AudioBytes.ToMemory(), mimeType)
            {
                AdditionalProperties = new() { [nameof(audio.ExpiresAt)] = audio.ExpiresAt },
            };

            if (audio.Id is string id)
            {
                dc.AdditionalProperties[nameof(audio.Id)] = id;
            }

            if (audio.Transcript is string transcript)
            {
                dc.AdditionalProperties[nameof(audio.Transcript)] = transcript;
            }

            returnMessage.Contents.Add(dc);
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

        // Wrap the content in a ChatResponse to return.
        var response = new ChatResponse([returnMessage])
        {
            CreatedAt = openAICompletion.CreatedAt,
            FinishReason = FromOpenAIFinishReason(openAICompletion.FinishReason),
            ModelId = openAICompletion.Model,
            RawRepresentation = openAICompletion,
            ResponseId = openAICompletion.Id,
        };

        if (openAICompletion.Usage is ChatTokenUsage tokenUsage)
        {
            response.Usage = FromOpenAIUsage(tokenUsage);
        }

        if (openAICompletion.ContentTokenLogProbabilities is { Count: > 0 } contentTokenLogProbs)
        {
            (response.AdditionalProperties ??= [])[nameof(openAICompletion.ContentTokenLogProbabilities)] = contentTokenLogProbs;
        }

        if (openAICompletion.Refusal is string refusal)
        {
            (response.AdditionalProperties ??= [])[nameof(openAICompletion.Refusal)] = refusal;
        }

        if (openAICompletion.RefusalTokenLogProbabilities is { Count: > 0 } refusalTokenLogProbs)
        {
            (response.AdditionalProperties ??= [])[nameof(openAICompletion.RefusalTokenLogProbabilities)] = refusalTokenLogProbs;
        }

        if (openAICompletion.SystemFingerprint is string systemFingerprint)
        {
            (response.AdditionalProperties ??= [])[nameof(openAICompletion.SystemFingerprint)] = systemFingerprint;
        }

        return response;
    }

    public static ChatOptions FromOpenAIOptions(ChatCompletionOptions? options)
    {
        ChatOptions result = new();

        if (options is not null)
        {
            result.ModelId = _getModelIdAccessor.Invoke(options, null)?.ToString() switch
            {
                null or "" => null,
                var modelId => modelId,
            };

            result.FrequencyPenalty = options.FrequencyPenalty;
            result.MaxOutputTokens = options.MaxOutputTokenCount;
            result.TopP = options.TopP;
            result.PresencePenalty = options.PresencePenalty;
            result.Temperature = options.Temperature;
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
            result.Seed = options.Seed;
#pragma warning restore OPENAI001

            if (options.StopSequences is { Count: > 0 } stopSequences)
            {
                result.StopSequences = [.. stopSequences];
            }

            if (options.EndUserId is string endUserId)
            {
                (result.AdditionalProperties ??= [])[nameof(options.EndUserId)] = endUserId;
            }

            if (options.IncludeLogProbabilities is bool includeLogProbabilities)
            {
                (result.AdditionalProperties ??= [])[nameof(options.IncludeLogProbabilities)] = includeLogProbabilities;
            }

            if (options.LogitBiases is { Count: > 0 } logitBiases)
            {
                (result.AdditionalProperties ??= [])[nameof(options.LogitBiases)] = new Dictionary<int, int>(logitBiases);
            }

            if (options.AllowParallelToolCalls is bool allowParallelToolCalls)
            {
                (result.AdditionalProperties ??= [])[nameof(options.AllowParallelToolCalls)] = allowParallelToolCalls;
            }

            if (options.TopLogProbabilityCount is int topLogProbabilityCount)
            {
                (result.AdditionalProperties ??= [])[nameof(options.TopLogProbabilityCount)] = topLogProbabilityCount;
            }

            if (options.Metadata is IDictionary<string, string> { Count: > 0 } metadata)
            {
                (result.AdditionalProperties ??= [])[nameof(options.Metadata)] = new Dictionary<string, string>(metadata);
            }

            if (options.StoredOutputEnabled is bool storedOutputEnabled)
            {
                (result.AdditionalProperties ??= [])[nameof(options.StoredOutputEnabled)] = storedOutputEnabled;
            }

            if (options.Tools is { Count: > 0 } tools)
            {
                foreach (ChatTool tool in tools)
                {
                    result.Tools ??= [];
                    result.Tools.Add(FromOpenAIChatTool(tool));
                }

                using var toolChoiceJson = JsonDocument.Parse(JsonModelHelpers.Serialize(options.ToolChoice).ToMemory());
                JsonElement jsonElement = toolChoiceJson.RootElement;
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.String:
                        result.ToolMode = jsonElement.GetString() switch
                        {
                            "required" => ChatToolMode.RequireAny,
                            "none" => ChatToolMode.None,
                            _ => ChatToolMode.Auto,
                        };

                        break;
                    case JsonValueKind.Object:
                        if (jsonElement.TryGetProperty("function", out JsonElement functionElement))
                        {
                            result.ToolMode = ChatToolMode.RequireSpecific(functionElement.GetString()!);
                        }

                        break;
                }
            }
        }

        return result;
    }

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    public static ChatCompletionOptions ToOpenAIOptions(ChatOptions? options)
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
                if (additionalProperties.TryGetValue(nameof(result.AllowParallelToolCalls), out bool allowParallelToolCalls))
                {
                    result.AllowParallelToolCalls = allowParallelToolCalls;
                }

                if (additionalProperties.TryGetValue(nameof(result.AudioOptions), out ChatAudioOptions? audioOptions))
                {
                    result.AudioOptions = audioOptions;
                }

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

                if (additionalProperties.TryGetValue(nameof(result.Metadata), out IDictionary<string, string>? metadata))
                {
                    foreach (KeyValuePair<string, string> kvp in metadata)
                    {
                        result.Metadata[kvp.Key] = kvp.Value;
                    }
                }

                if (additionalProperties.TryGetValue(nameof(result.OutputPrediction), out ChatOutputPrediction? outputPrediction))
                {
                    result.OutputPrediction = outputPrediction;
                }

                if (additionalProperties.TryGetValue(nameof(result.ReasoningEffortLevel), out ChatReasoningEffortLevel reasoningEffortLevel))
                {
                    result.ReasoningEffortLevel = reasoningEffortLevel;
                }

                if (additionalProperties.TryGetValue(nameof(result.ResponseModalities), out ChatResponseModalities responseModalities))
                {
                    result.ResponseModalities = responseModalities;
                }

                if (additionalProperties.TryGetValue(nameof(result.StoredOutputEnabled), out bool storeOutputEnabled))
                {
                    result.StoredOutputEnabled = storeOutputEnabled;
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
                    case NoneChatToolMode:
                        result.ToolChoice = ChatToolChoice.CreateNoneChoice();
                        break;

                    case AutoChatToolMode:
                    case null:
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
                result.ResponseFormat = jsonFormat.Schema is { } jsonSchema ?
                    OpenAI.Chat.ChatResponseFormat.CreateJsonSchemaFormat(
                        jsonFormat.SchemaName ?? "json_schema",
                        BinaryData.FromBytes(
                            JsonSerializer.SerializeToUtf8Bytes(jsonSchema, OpenAIJsonContext.Default.JsonElement)),
                        jsonFormat.SchemaDescription) :
                    OpenAI.Chat.ChatResponseFormat.CreateJsonObjectFormat();
            }
        }

        return result;
    }

    private static AITool FromOpenAIChatTool(ChatTool chatTool)
    {
        AdditionalPropertiesDictionary additionalProperties = [];
        if (chatTool.FunctionSchemaIsStrict is bool strictValue)
        {
            additionalProperties["Strict"] = strictValue;
        }

        OpenAIChatToolJson openAiChatTool = JsonSerializer.Deserialize(chatTool.FunctionParameters.ToMemory().Span, OpenAIJsonContext.Default.OpenAIChatToolJson)!;
        List<AIFunctionParameterMetadata> parameters = new(openAiChatTool.Properties.Count);
        foreach (KeyValuePair<string, JsonElement> property in openAiChatTool.Properties)
        {
            parameters.Add(new(property.Key)
            {
                IsRequired = openAiChatTool.Required.Contains(property.Key),
            });
        }

        AIFunctionMetadata metadata = new(chatTool.FunctionName)
        {
            Description = chatTool.FunctionDescription,
            AdditionalProperties = additionalProperties,
            Parameters = parameters,
            Schema = JsonSerializer.SerializeToElement(openAiChatTool, OpenAIJsonContext.Default.OpenAIChatToolJson),
            ReturnParameter = new()
            {
                Description = "Return parameter",
                Schema = DefaultParameterSchema,
            }
        };

        return new MetadataOnlyAIFunction(metadata);
    }

    private sealed class MetadataOnlyAIFunction(AIFunctionMetadata metadata) : AIFunction
    {
        public override AIFunctionMetadata Metadata => metadata;
        protected override Task<object?> InvokeCoreAsync(IEnumerable<KeyValuePair<string, object?>> arguments, CancellationToken cancellationToken) =>
            throw new InvalidOperationException($"The AI function '{metadata.Name}' does not support being invoked.");
    }

    /// <summary>Converts an Extensions function to an OpenAI chat tool.</summary>
    private static ChatTool ToOpenAIChatTool(AIFunction aiFunction)
    {
        bool? strict =
            aiFunction.Metadata.AdditionalProperties.TryGetValue("Strict", out object? strictObj) &&
            strictObj is bool strictValue ?
            strictValue : null;

        // Map to an intermediate model so that redundant properties are skipped.
        var tool = JsonSerializer.Deserialize(aiFunction.Metadata.Schema, OpenAIJsonContext.Default.OpenAIChatToolJson)!;
        var functionParameters = BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(tool, OpenAIJsonContext.Default.OpenAIChatToolJson));
        return ChatTool.CreateFunctionTool(aiFunction.Metadata.Name, aiFunction.Metadata.Description, functionParameters, strict);
    }

    private static UsageDetails FromOpenAIUsage(ChatTokenUsage tokenUsage)
    {
        var destination = new UsageDetails
        {
            InputTokenCount = tokenUsage.InputTokenCount,
            OutputTokenCount = tokenUsage.OutputTokenCount,
            TotalTokenCount = tokenUsage.TotalTokenCount,
            AdditionalCounts = [],
        };

        var counts = destination.AdditionalCounts;

        if (tokenUsage.InputTokenDetails is ChatInputTokenUsageDetails inputDetails)
        {
            const string InputDetails = nameof(ChatTokenUsage.InputTokenDetails);
            counts.Add($"{InputDetails}.{nameof(ChatInputTokenUsageDetails.AudioTokenCount)}", inputDetails.AudioTokenCount);
            counts.Add($"{InputDetails}.{nameof(ChatInputTokenUsageDetails.CachedTokenCount)}", inputDetails.CachedTokenCount);
        }

        if (tokenUsage.OutputTokenDetails is ChatOutputTokenUsageDetails outputDetails)
        {
            const string OutputDetails = nameof(ChatTokenUsage.OutputTokenDetails);
            counts.Add($"{OutputDetails}.{nameof(ChatOutputTokenUsageDetails.ReasoningTokenCount)}", outputDetails.ReasoningTokenCount);
            counts.Add($"{OutputDetails}.{nameof(ChatOutputTokenUsageDetails.AudioTokenCount)}", outputDetails.AudioTokenCount);
            counts.Add($"{OutputDetails}.{nameof(ChatOutputTokenUsageDetails.AcceptedPredictionTokenCount)}", outputDetails.AcceptedPredictionTokenCount);
            counts.Add($"{OutputDetails}.{nameof(ChatOutputTokenUsageDetails.RejectedPredictionTokenCount)}", outputDetails.RejectedPredictionTokenCount);
        }

        return destination;
    }

    private static ChatTokenUsage ToOpenAIUsage(UsageDetails usageDetails)
    {
        ChatOutputTokenUsageDetails? outputTokenUsageDetails = null;
        ChatInputTokenUsageDetails? inputTokenUsageDetails = null;

        if (usageDetails.AdditionalCounts is { Count: > 0 } additionalCounts)
        {
            const string InputDetails = nameof(ChatTokenUsage.InputTokenDetails);
            if (additionalCounts.TryGetValue($"{InputDetails}.{nameof(ChatInputTokenUsageDetails.AudioTokenCount)}", out int inputAudioTokenCount) |
                additionalCounts.TryGetValue($"{InputDetails}.{nameof(ChatInputTokenUsageDetails.CachedTokenCount)}", out int inputCachedTokenCount))
            {
                inputTokenUsageDetails = OpenAIChatModelFactory.ChatInputTokenUsageDetails(
                    audioTokenCount: inputAudioTokenCount,
                    cachedTokenCount: inputCachedTokenCount);
            }

            const string OutputDetails = nameof(ChatTokenUsage.OutputTokenDetails);
            if (additionalCounts.TryGetValue($"{OutputDetails}.{nameof(ChatOutputTokenUsageDetails.ReasoningTokenCount)}", out int outputReasoningTokenCount) |
                additionalCounts.TryGetValue($"{OutputDetails}.{nameof(ChatOutputTokenUsageDetails.AudioTokenCount)}", out int outputAudioTokenCount) |
                additionalCounts.TryGetValue($"{OutputDetails}.{nameof(ChatOutputTokenUsageDetails.AcceptedPredictionTokenCount)}", out int outputAcceptedPredictionCount) |
                additionalCounts.TryGetValue($"{OutputDetails}.{nameof(ChatOutputTokenUsageDetails.RejectedPredictionTokenCount)}", out int outputRejectedPredictionCount))
            {
                outputTokenUsageDetails = OpenAIChatModelFactory.ChatOutputTokenUsageDetails(
                    reasoningTokenCount: outputReasoningTokenCount,
                    audioTokenCount: outputAudioTokenCount,
                    acceptedPredictionTokenCount: outputAcceptedPredictionCount,
                    rejectedPredictionTokenCount: outputRejectedPredictionCount);
            }
        }

        return OpenAIChatModelFactory.ChatTokenUsage(
            inputTokenCount: ToInt32Saturate(usageDetails.InputTokenCount),
            outputTokenCount: ToInt32Saturate(usageDetails.OutputTokenCount),
            totalTokenCount: ToInt32Saturate(usageDetails.TotalTokenCount),
            outputTokenDetails: outputTokenUsageDetails,
            inputTokenDetails: inputTokenUsageDetails);

        static int ToInt32Saturate(long? value) =>
            value is null ? 0 :
            value > int.MaxValue ? int.MaxValue :
            value < int.MinValue ? int.MinValue :
            (int)value;
    }

    /// <summary>Converts an OpenAI role to an Extensions role.</summary>
    private static ChatRole FromOpenAIChatRole(ChatMessageRole role) =>
        role switch
        {
            ChatMessageRole.System => ChatRole.System,
            ChatMessageRole.User => ChatRole.User,
            ChatMessageRole.Assistant => ChatRole.Assistant,
            ChatMessageRole.Tool => ChatRole.Tool,
            ChatMessageRole.Developer => ChatRoleDeveloper,
            _ => new ChatRole(role.ToString()),
        };

    /// <summary>Converts an Extensions role to an OpenAI role.</summary>
    [return: NotNullIfNotNull("role")]
    private static ChatMessageRole? ToOpenAIChatRole(ChatRole? role) =>
        role is null ? null :
        role == ChatRole.System ? ChatMessageRole.System :
        role == ChatRole.User ? ChatMessageRole.User :
        role == ChatRole.Assistant ? ChatMessageRole.Assistant :
        role == ChatRole.Tool ? ChatMessageRole.Tool :
        role == OpenAIModelMappers.ChatRoleDeveloper ? ChatMessageRole.Developer :
        ChatMessageRole.User;

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
            DataContent? imageContent;
            aiContent = imageContent =
                contentPart.ImageUri is not null ? new DataContent(contentPart.ImageUri, contentPart.ImageBytesMediaType) :
                contentPart.ImageBytes is not null ? new DataContent(contentPart.ImageBytes.ToMemory(), contentPart.ImageBytesMediaType) :
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
    private static ChatFinishReason? FromOpenAIFinishReason(OpenAI.Chat.ChatFinishReason? finishReason) =>
        finishReason?.ToString() is not string s ? null :
        finishReason switch
        {
            OpenAI.Chat.ChatFinishReason.Stop => ChatFinishReason.Stop,
            OpenAI.Chat.ChatFinishReason.Length => ChatFinishReason.Length,
            OpenAI.Chat.ChatFinishReason.ContentFilter => ChatFinishReason.ContentFilter,
            OpenAI.Chat.ChatFinishReason.ToolCalls or OpenAI.Chat.ChatFinishReason.FunctionCall => ChatFinishReason.ToolCalls,
            _ => new ChatFinishReason(s),
        };

    /// <summary>Converts an Extensions finish reason to an OpenAI finish reason.</summary>
    private static OpenAI.Chat.ChatFinishReason ToOpenAIFinishReason(ChatFinishReason? finishReason) =>
        finishReason == ChatFinishReason.Length ? OpenAI.Chat.ChatFinishReason.Length :
        finishReason == ChatFinishReason.ContentFilter ? OpenAI.Chat.ChatFinishReason.ContentFilter :
        finishReason == ChatFinishReason.ToolCalls ? OpenAI.Chat.ChatFinishReason.ToolCalls :
        OpenAI.Chat.ChatFinishReason.Stop;

    private static FunctionCallContent ParseCallContentFromJsonString(string json, string callId, string name) =>
        FunctionCallContent.CreateFromParsedArguments(json, callId, name,
            argumentParser: static json => JsonSerializer.Deserialize(json, OpenAIJsonContext.Default.IDictionaryStringObject)!);

    private static FunctionCallContent ParseCallContentFromBinaryData(BinaryData ut8Json, string callId, string name) =>
        FunctionCallContent.CreateFromParsedArguments(ut8Json, callId, name,
            argumentParser: static json => JsonSerializer.Deserialize(json, OpenAIJsonContext.Default.IDictionaryStringObject)!);

    private static T? GetValueOrDefault<T>(this AdditionalPropertiesDictionary? dict, string key) =>
        dict?.TryGetValue(key, out T? value) is true ? value : default;

    private static string CreateCompletionId() => $"chatcmpl-{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}";

    /// <summary>Used to create the JSON payload for an OpenAI chat tool description.</summary>
    public sealed class OpenAIChatToolJson
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";

        [JsonPropertyName("required")]
        public HashSet<string> Required { get; set; } = [];

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
