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

#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable S103 // Lines should not be too long
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S3440 // Variables should not be checked against the values they're about to be assigned

namespace Microsoft.Extensions.AI;

internal static partial class OpenAIModelMappers
{
    private static readonly JsonElement _defaultParameterSchema = JsonDocument.Parse("{}").RootElement;

    public static OpenAI.Chat.ChatCompletion ToOpenAIChatCompletion(ChatCompletion chatCompletion, JsonSerializerOptions options)
    {
        _ = Throw.IfNull(chatCompletion);

        if (chatCompletion.Choices.Count > 1)
        {
            throw new NotSupportedException("Creating OpenAI ChatCompletion models with multiple choices is currently not supported.");
        }

        List<OpenAI.Chat.ChatToolCall>? toolCalls = null;
        foreach (AIContent content in chatCompletion.Message.Contents)
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

        OpenAI.Chat.ChatTokenUsage? chatTokenUsage = null;
        if (chatCompletion.Usage is UsageDetails usageDetails)
        {
            chatTokenUsage = ToOpenAIUsage(usageDetails);
        }

        return OpenAIChatModelFactory.ChatCompletion(
            id: chatCompletion.CompletionId ?? CreateCompletionId(),
            model: chatCompletion.ModelId,
            createdAt: chatCompletion.CreatedAt ?? DateTimeOffset.UtcNow,
            role: ToOpenAIChatRole(chatCompletion.Message.Role).Value,
            finishReason: ToOpenAIFinishReason(chatCompletion.FinishReason),
            content: new(ToOpenAIChatContent(chatCompletion.Message.Contents)),
            toolCalls: toolCalls,
            refusal: chatCompletion.AdditionalProperties.GetValueOrDefault<string>(nameof(OpenAI.Chat.ChatCompletion.Refusal)),
            contentTokenLogProbabilities: chatCompletion.AdditionalProperties.GetValueOrDefault<IReadOnlyList<ChatTokenLogProbabilityDetails>>(nameof(OpenAI.Chat.ChatCompletion.ContentTokenLogProbabilities)),
            refusalTokenLogProbabilities: chatCompletion.AdditionalProperties.GetValueOrDefault<IReadOnlyList<ChatTokenLogProbabilityDetails>>(nameof(OpenAI.Chat.ChatCompletion.RefusalTokenLogProbabilities)),
            systemFingerprint: chatCompletion.AdditionalProperties.GetValueOrDefault<string>(nameof(OpenAI.Chat.ChatCompletion.SystemFingerprint)),
            usage: chatTokenUsage);
    }

    public static ChatCompletion FromOpenAIChatCompletion(OpenAI.Chat.ChatCompletion openAICompletion, ChatOptions? options)
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
            FinishReason = FromOpenAIFinishReason(openAICompletion.FinishReason),
        };

        if (openAICompletion.Usage is ChatTokenUsage tokenUsage)
        {
            completion.Usage = FromOpenAIUsage(tokenUsage);
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

    public static ChatOptions FromOpenAIOptions(OpenAI.Chat.ChatCompletionOptions? options)
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
    public static OpenAI.Chat.ChatCompletionOptions ToOpenAIOptions(ChatOptions? options)
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

                if (additionalProperties.TryGetValue(nameof(result.Metadata), out IDictionary<string, string>? metadata))
                {
                    foreach (KeyValuePair<string, string> kvp in metadata)
                    {
                        result.Metadata[kvp.Key] = kvp.Value;
                    }
                }

                if (additionalProperties.TryGetValue(nameof(result.StoredOutputEnabled), out bool storeOutputEnabled))
                {
                    result.StoredOutputEnabled = storeOutputEnabled;
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
        AdditionalPropertiesDictionary additionalProperties = new();
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
                Schema = property.Value,
                IsRequired = openAiChatTool.Required.Contains(property.Key),
            });
        }

        AIFunctionMetadata metadata = new(chatTool.FunctionName)
        {
            Description = chatTool.FunctionDescription,
            AdditionalProperties = additionalProperties,
            Parameters = parameters,
            ReturnParameter = new()
            {
                Description = "Return parameter",
                Schema = _defaultParameterSchema,
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
                    _ = tool.Required.Add(parameter.Name);
                }
            }

            resultParameters = BinaryData.FromBytes(
                JsonSerializer.SerializeToUtf8Bytes(tool, OpenAIJsonContext.Default.OpenAIChatToolJson));
        }

        return ChatTool.CreateFunctionTool(aiFunction.Metadata.Name, aiFunction.Metadata.Description, resultParameters, strict);
    }

    private static UsageDetails FromOpenAIUsage(ChatTokenUsage tokenUsage)
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
            destination.AdditionalCounts.Add(
                $"{nameof(ChatTokenUsage.InputTokenDetails)}.{nameof(ChatInputTokenUsageDetails.AudioTokenCount)}",
                inputDetails.AudioTokenCount);

            destination.AdditionalCounts.Add(
                $"{nameof(ChatTokenUsage.InputTokenDetails)}.{nameof(ChatInputTokenUsageDetails.CachedTokenCount)}",
                inputDetails.CachedTokenCount);
        }

        if (tokenUsage.OutputTokenDetails is ChatOutputTokenUsageDetails outputDetails)
        {
            destination.AdditionalCounts.Add(
                $"{nameof(ChatTokenUsage.OutputTokenDetails)}.{nameof(ChatOutputTokenUsageDetails.AudioTokenCount)}",
                outputDetails.AudioTokenCount);

            destination.AdditionalCounts.Add(
                $"{nameof(ChatTokenUsage.OutputTokenDetails)}.{nameof(ChatOutputTokenUsageDetails.ReasoningTokenCount)}",
                outputDetails.ReasoningTokenCount);
        }

        return destination;
    }

    private static ChatTokenUsage ToOpenAIUsage(UsageDetails usageDetails)
    {
        ChatOutputTokenUsageDetails? outputTokenUsageDetails = null;
        ChatInputTokenUsageDetails? inputTokenUsageDetails = null;

        if (usageDetails.AdditionalCounts is { Count: > 0 } additionalCounts)
        {
            int? inputAudioTokenCount = additionalCounts.TryGetValue(
                $"{nameof(ChatTokenUsage.InputTokenDetails)}.{nameof(ChatInputTokenUsageDetails.AudioTokenCount)}",
                out int value) ? value : null;

            int? inputCachedTokenCount = additionalCounts.TryGetValue(
                $"{nameof(ChatTokenUsage.InputTokenDetails)}.{nameof(ChatInputTokenUsageDetails.CachedTokenCount)}",
                out value) ? value : null;

            int? outputAudioTokenCount = additionalCounts.TryGetValue(
                $"{nameof(ChatTokenUsage.OutputTokenDetails)}.{nameof(ChatOutputTokenUsageDetails.AudioTokenCount)}",
                out value) ? value : null;

            int? outputReasoningTokenCount = additionalCounts.TryGetValue(
                $"{nameof(ChatTokenUsage.OutputTokenDetails)}.{nameof(ChatOutputTokenUsageDetails.ReasoningTokenCount)}",
                out value) ? value : null;

            if (inputAudioTokenCount is not null || inputCachedTokenCount is not null)
            {
                inputTokenUsageDetails = OpenAIChatModelFactory.ChatInputTokenUsageDetails(
                    audioTokenCount: inputAudioTokenCount ?? 0,
                    cachedTokenCount: inputCachedTokenCount ?? 0);
            }

            if (outputAudioTokenCount is not null || outputReasoningTokenCount is not null)
            {
                outputTokenUsageDetails = OpenAIChatModelFactory.ChatOutputTokenUsageDetails(
                    audioTokenCount: outputAudioTokenCount ?? 0,
                    reasoningTokenCount: outputReasoningTokenCount ?? 0);
            }
        }

        return OpenAIChatModelFactory.ChatTokenUsage(
            inputTokenCount: usageDetails.InputTokenCount ?? 0,
            outputTokenCount: usageDetails.OutputTokenCount ?? 0,
            totalTokenCount: usageDetails.TotalTokenCount ?? 0,
            outputTokenDetails: outputTokenUsageDetails,
            inputTokenDetails: inputTokenUsageDetails);
    }

    /// <summary>Converts an OpenAI role to an Extensions role.</summary>
    private static ChatRole FromOpenAIChatRole(ChatMessageRole role) =>
        role switch
        {
            ChatMessageRole.System => ChatRole.System,
            ChatMessageRole.User => ChatRole.User,
            ChatMessageRole.Assistant => ChatRole.Assistant,
            ChatMessageRole.Tool => ChatRole.Tool,
            _ => new ChatRole(role.ToString()),
        };

    /// <summary>Converts an Extensions role to an OpenAI role.</summary>
    [return: NotNullIfNotNull("role")]
    private static ChatMessageRole? ToOpenAIChatRole(ChatRole? role) =>
        role is null ? null :
        role == ChatRole.System ? ChatMessageRole.System :
        role == ChatRole.User ? ChatMessageRole.User :
        role == ChatRole.Assistant ? ChatMessageRole.Assistant :
        role == ChatRole.Tool ? ChatMessageRole.Tool : ChatMessageRole.User;

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
        /// <summary>Gets a singleton JSON data for empty parameters. Optimization for the reasonably common case of a parameterless function.</summary>
        public static BinaryData ZeroFunctionParametersSchema { get; } = new("""{"type":"object","required":[],"properties":{}}"""u8.ToArray());

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
