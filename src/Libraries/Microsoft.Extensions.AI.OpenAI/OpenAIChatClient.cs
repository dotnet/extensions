// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI;
using OpenAI.Chat;

#pragma warning disable S1135 // Track uses of "TODO" tags
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1108 // Block statements should not contain embedded comments

namespace Microsoft.Extensions.AI;

/// <summary>An <see cref="IChatClient"/> for an OpenAI <see cref="OpenAIClient"/> or <see cref="OpenAI.Chat.ChatClient"/>.</summary>
public sealed partial class OpenAIChatClient : IChatClient
{
    private static readonly JsonElement _defaultParameterSchema = JsonDocument.Parse("{}").RootElement;

    /// <summary>Default OpenAI endpoint.</summary>
    private static readonly Uri _defaultOpenAIEndpoint = new("https://api.openai.com/v1");

    /// <summary>The underlying <see cref="OpenAIClient" />.</summary>
    private readonly OpenAIClient? _openAIClient;

    /// <summary>The underlying <see cref="ChatClient" />.</summary>
    private readonly ChatClient _chatClient;

    /// <summary>Initializes a new instance of the <see cref="OpenAIChatClient"/> class for the specified <see cref="OpenAIClient"/>.</summary>
    /// <param name="openAIClient">The underlying client.</param>
    /// <param name="modelId">The model to use.</param>
    public OpenAIChatClient(OpenAIClient openAIClient, string modelId)
    {
        _ = Throw.IfNull(openAIClient);
        _ = Throw.IfNullOrWhitespace(modelId);

        _openAIClient = openAIClient;
        _chatClient = openAIClient.GetChatClient(modelId);

        // https://github.com/openai/openai-dotnet/issues/215
        // The endpoint isn't currently exposed, so use reflection to get at it, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        Uri providerUrl = typeof(OpenAIClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(openAIClient) as Uri ?? _defaultOpenAIEndpoint;

        Metadata = new("openai", providerUrl, modelId);
    }

    /// <summary>Initializes a new instance of the <see cref="OpenAIChatClient"/> class for the specified <see cref="ChatClient"/>.</summary>
    /// <param name="chatClient">The underlying client.</param>
    public OpenAIChatClient(ChatClient chatClient)
    {
        _ = Throw.IfNull(chatClient);

        _chatClient = chatClient;

        // https://github.com/openai/openai-dotnet/issues/215
        // The endpoint and model aren't currently exposed, so use reflection to get at them, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        Uri providerUrl = typeof(ChatClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(chatClient) as Uri ?? _defaultOpenAIEndpoint;
        string? model = typeof(ChatClient).GetField("_model", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(chatClient) as string;

        Metadata = new("openai", providerUrl, model);
    }

    /// <summary>Gets or sets <see cref="JsonSerializerOptions"/> to use for any serialization activities related to tool call arguments and results.</summary>
    public JsonSerializerOptions? ToolCallJsonSerializerOptions { get; set; }

    /// <inheritdoc />
    public ChatClientMetadata Metadata { get; }

    /// <inheritdoc />
    public TService? GetService<TService>(object? key = null)
        where TService : class =>
        typeof(TService) == typeof(OpenAIClient) ? (TService?)(object?)_openAIClient :
        typeof(TService) == typeof(ChatClient) ? (TService)(object)_chatClient :
        this as TService;

    /// <inheritdoc />
    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        // Make the call to OpenAI.
        OpenAI.Chat.ChatCompletion response = (await _chatClient.CompleteChatAsync(
            ToOpenAIChatMessages(chatMessages),
            ToOpenAIOptions(options),
            cancellationToken).ConfigureAwait(false)).Value;

        // Create the return message.
        ChatMessage returnMessage = new()
        {
            RawRepresentation = response,
            Role = ToChatRole(response.Role),
        };

        // Populate its content from those in the OpenAI response content.
        foreach (ChatMessageContentPart contentPart in response.Content)
        {
            if (ToAIContent(contentPart) is AIContent aiContent)
            {
                returnMessage.Contents.Add(aiContent);
            }
        }

        // Also manufacture function calling content items from any tool calls in the response.
        if (options?.Tools is { Count: > 0 })
        {
            foreach (ChatToolCall toolCall in response.ToolCalls)
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
            RawRepresentation = response,
            CompletionId = response.Id,
            CreatedAt = response.CreatedAt,
            ModelId = response.Model,
            FinishReason = ToFinishReason(response.FinishReason),
        };

        if (response.Usage is ChatTokenUsage tokenUsage)
        {
            completion.Usage = new()
            {
                InputTokenCount = tokenUsage.InputTokenCount,
                OutputTokenCount = tokenUsage.OutputTokenCount,
                TotalTokenCount = tokenUsage.TotalTokenCount,
            };

            if (tokenUsage.OutputTokenDetails is ChatOutputTokenUsageDetails details)
            {
                completion.Usage.AdditionalProperties = new() { [nameof(details.ReasoningTokenCount)] = details.ReasoningTokenCount };
            }
        }

        if (response.ContentTokenLogProbabilities is { Count: > 0 } contentTokenLogProbs)
        {
            (completion.AdditionalProperties ??= [])[nameof(response.ContentTokenLogProbabilities)] = contentTokenLogProbs;
        }

        if (response.Refusal is string refusal)
        {
            (completion.AdditionalProperties ??= [])[nameof(response.Refusal)] = refusal;
        }

        if (response.RefusalTokenLogProbabilities is { Count: > 0 } refusalTokenLogProbs)
        {
            (completion.AdditionalProperties ??= [])[nameof(response.RefusalTokenLogProbabilities)] = refusalTokenLogProbs;
        }

        if (response.SystemFingerprint is string systemFingerprint)
        {
            (completion.AdditionalProperties ??= [])[nameof(response.SystemFingerprint)] = systemFingerprint;
        }

        return completion;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        Dictionary<int, FunctionCallInfo>? functionCallInfos = null;
        ChatRole? streamedRole = null;
        ChatFinishReason? finishReason = null;
        StringBuilder? refusal = null;
        string? completionId = null;
        DateTimeOffset? createdAt = null;
        string? modelId = null;
        string? fingerprint = null;

        // Process each update as it arrives
        await foreach (OpenAI.Chat.StreamingChatCompletionUpdate chatCompletionUpdate in _chatClient.CompleteChatStreamingAsync(
            ToOpenAIChatMessages(chatMessages), ToOpenAIOptions(options), cancellationToken).ConfigureAwait(false))
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
                UsageDetails usageDetails = new()
                {
                    InputTokenCount = tokenUsage.InputTokenCount,
                    OutputTokenCount = tokenUsage.OutputTokenCount,
                    TotalTokenCount = tokenUsage.TotalTokenCount,
                };

                if (tokenUsage.OutputTokenDetails is ChatOutputTokenUsageDetails details)
                {
                    (usageDetails.AdditionalProperties = [])[nameof(tokenUsage.OutputTokenDetails)] = new Dictionary<string, object?>
                    {
                        [nameof(details.ReasoningTokenCount)] = details.ReasoningTokenCount,
                    };
                }

                // TODO: Add support for prompt token details (e.g. cached tokens) once it's exposed in OpenAI library.

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

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IChatClient interface.
    }

    /// <summary>POCO representing function calling info. Used to concatenation information for a single function call from across multiple streaming updates.</summary>
    private sealed class FunctionCallInfo
    {
        public string? CallId;
        public string? Name;
        public StringBuilder? Arguments;
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

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    private static ChatCompletionOptions ToOpenAIOptions(ChatOptions? options)
    {
        ChatCompletionOptions result = new();

        if (options is not null)
        {
            result.FrequencyPenalty = options.FrequencyPenalty;
            result.MaxOutputTokenCount = options.MaxOutputTokens;
            result.TopP = options.TopP;
            result.PresencePenalty = options.PresencePenalty;
            result.Temperature = options.Temperature;

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

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                if (additionalProperties.TryGetValue(nameof(result.Seed), out long seed))
                {
                    result.Seed = seed;
                }
#pragma warning restore OPENAI001

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
                JsonSerializer.SerializeToUtf8Bytes(tool, JsonContext.Default.OpenAIChatToolJson));
        }

        return ChatTool.CreateFunctionTool(aiFunction.Metadata.Name, aiFunction.Metadata.Description, resultParameters, strict);
    }

    /// <summary>Used to create the JSON payload for an OpenAI chat tool description.</summary>
    private sealed class OpenAIChatToolJson
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

    /// <summary>Converts an Extensions chat message enumerable to an OpenAI chat message enumerable.</summary>
    private IEnumerable<OpenAI.Chat.ChatMessage> ToOpenAIChatMessages(IEnumerable<ChatMessage> inputs)
    {
        // Maps all of the M.E.AI types to the corresponding OpenAI types.
        // Unrecognized or non-processable content is ignored.

        foreach (ChatMessage input in inputs)
        {
            if (input.Role == ChatRole.System || input.Role == ChatRole.User)
            {
                var parts = GetContentParts(input.Contents);
                yield return input.Role == ChatRole.System ?
                    new SystemChatMessage(parts) { ParticipantName = input.AuthorName } :
                    new UserChatMessage(parts) { ParticipantName = input.AuthorName };
            }
            else if (input.Role == ChatRole.Tool)
            {
                foreach (AIContent item in input.Contents)
                {
                    if (item is FunctionResultContent resultContent)
                    {
                        string? result = resultContent.Result as string;
                        if (result is null && resultContent.Result is not null)
                        {
                            JsonSerializerOptions options = ToolCallJsonSerializerOptions ?? JsonContext.Default.Options;
                            try
                            {
                                result = JsonSerializer.Serialize(resultContent.Result, options.GetTypeInfo(typeof(object)));
                            }
                            catch (NotSupportedException)
                            {
                                // If the type can't be serialized, skip it.
                            }
                        }

                        yield return new ToolChatMessage(resultContent.CallId, result ?? string.Empty);
                    }
                }
            }
            else if (input.Role == ChatRole.Assistant)
            {
                AssistantChatMessage message = new(GetContentParts(input.Contents))
                {
                    ParticipantName = input.AuthorName
                };

                foreach (var content in input.Contents)
                {
                    if (content is FunctionCallContent { CallId: not null } callRequest)
                    {
                        message.ToolCalls.Add(
                            ChatToolCall.CreateFunctionToolCall(
                                callRequest.CallId,
                                callRequest.Name,
                                BinaryData.FromObjectAsJson(callRequest.Arguments, ToolCallJsonSerializerOptions)));
                    }
                }

                if (input.AdditionalProperties?.TryGetValue(nameof(message.Refusal), out string? refusal) is true)
                {
                    message.Refusal = refusal;
                }

                yield return message;
            }
        }
    }

    /// <summary>Converts a list of <see cref="AIContent"/> to a list of <see cref="ChatMessageContentPart"/>.</summary>
    private static List<ChatMessageContentPart> GetContentParts(IList<AIContent> contents)
    {
        List<ChatMessageContentPart> parts = [];
        foreach (var content in contents)
        {
            switch (content)
            {
                case TextContent textContent:
                    parts.Add(ChatMessageContentPart.CreateTextPart(textContent.Text));
                    break;

                case ImageContent imageContent when imageContent.Data is { IsEmpty: false } data:
                    parts.Add(ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(data), imageContent.MediaType));
                    break;

                case ImageContent imageContent when imageContent.Uri is string uri:
                    parts.Add(ChatMessageContentPart.CreateImagePart(new Uri(uri)));
                    break;
            }
        }

        if (parts.Count == 0)
        {
            parts.Add(ChatMessageContentPart.CreateTextPart(string.Empty));
        }

        return parts;
    }

    private static FunctionCallContent ParseCallContentFromJsonString(string json, string callId, string name) =>
        FunctionCallContent.CreateFromParsedArguments(json, callId, name,
            argumentParser: static json => JsonSerializer.Deserialize(json, JsonContext.Default.IDictionaryStringObject)!);

    private static FunctionCallContent ParseCallContentFromBinaryData(BinaryData ut8Json, string callId, string name) =>
        FunctionCallContent.CreateFromParsedArguments(ut8Json, callId, name,
            argumentParser: static json => JsonSerializer.Deserialize(json, JsonContext.Default.IDictionaryStringObject)!);

    /// <summary>Source-generated JSON type information.</summary>
    [JsonSerializable(typeof(OpenAIChatToolJson))]
    [JsonSerializable(typeof(IDictionary<string, object?>))]
    [JsonSerializable(typeof(JsonElement))]
    private sealed partial class JsonContext : JsonSerializerContext;
}
