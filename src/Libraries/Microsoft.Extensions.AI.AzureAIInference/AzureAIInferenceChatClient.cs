// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.Inference;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S1135 // Track uses of "TODO" tags
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Microsoft.Extensions.AI;

/// <summary>An <see cref="IChatClient"/> for an Azure AI Inference <see cref="ChatCompletionsClient"/>.</summary>
public sealed partial class AzureAIInferenceChatClient : IChatClient
{
    private static readonly JsonElement _defaultParameterSchema = JsonDocument.Parse("{}").RootElement;

    /// <summary>The underlying <see cref="ChatCompletionsClient" />.</summary>
    private readonly ChatCompletionsClient _chatCompletionsClient;

    /// <summary>Initializes a new instance of the <see cref="AzureAIInferenceChatClient"/> class for the specified <see cref="ChatCompletionsClient"/>.</summary>
    /// <param name="chatCompletionsClient">The underlying client.</param>
    /// <param name="modelId">The id of the model to use. If null, it may be provided per request via <see cref="ChatOptions.ModelId"/>.</param>
    public AzureAIInferenceChatClient(ChatCompletionsClient chatCompletionsClient, string? modelId = null)
    {
        _ = Throw.IfNull(chatCompletionsClient);
        if (modelId is not null)
        {
            _ = Throw.IfNullOrWhitespace(modelId);
        }

        _chatCompletionsClient = chatCompletionsClient;

        // https://github.com/Azure/azure-sdk-for-net/issues/46278
        // The endpoint isn't currently exposed, so use reflection to get at it, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        var providerUrl = typeof(ChatCompletionsClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(chatCompletionsClient) as Uri;

        Metadata = new("az.ai.inference", providerUrl, modelId);
    }

    /// <summary>Gets or sets <see cref="JsonSerializerOptions"/> to use for any serialization activities related to tool call arguments and results.</summary>
    public JsonSerializerOptions? ToolCallJsonSerializerOptions { get; set; }

    /// <inheritdoc />
    public ChatClientMetadata Metadata { get; }

    /// <inheritdoc />
    public TService? GetService<TService>(object? key = null)
        where TService : class =>
        typeof(TService) == typeof(ChatCompletionsClient) ? (TService?)(object?)_chatCompletionsClient :
        this as TService;

    /// <inheritdoc />
    public async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        // Make the call.
        ChatCompletions response = (await _chatCompletionsClient.CompleteAsync(
            ToAzureAIOptions(chatMessages, options),
            cancellationToken: cancellationToken).ConfigureAwait(false)).Value;

        // Create the return message.
        List<ChatMessage> returnMessages = [];

        // Populate its content from those in the response content.
        ChatFinishReason? finishReason = null;
        foreach (var choice in response.Choices)
        {
            ChatMessage returnMessage = new()
            {
                RawRepresentation = choice,
                Role = ToChatRole(choice.Message.Role),
                AdditionalProperties = new() { [nameof(choice.Index)] = choice.Index },
            };

            finishReason ??= ToFinishReason(choice.FinishReason);

            if (choice.Message.ToolCalls is { Count: > 0 } toolCalls)
            {
                foreach (var toolCall in toolCalls)
                {
                    if (toolCall is ChatCompletionsFunctionToolCall ftc && !string.IsNullOrWhiteSpace(ftc.Name))
                    {
                        FunctionCallContent callContent = ParseCallContentFromJsonString(ftc.Arguments, toolCall.Id, ftc.Name);
                        callContent.ModelId = response.Model;
                        callContent.RawRepresentation = toolCall;

                        returnMessage.Contents.Add(callContent);
                    }
                }
            }

            if (!string.IsNullOrEmpty(choice.Message.Content))
            {
                returnMessage.Contents.Add(new TextContent(choice.Message.Content)
                {
                    ModelId = response.Model,
                    RawRepresentation = choice.Message
                });
            }

            returnMessages.Add(returnMessage);
        }

        UsageDetails? usage = null;
        if (response.Usage is CompletionsUsage completionsUsage)
        {
            usage = new()
            {
                InputTokenCount = completionsUsage.PromptTokens,
                OutputTokenCount = completionsUsage.CompletionTokens,
                TotalTokenCount = completionsUsage.TotalTokens,
            };
        }

        // Wrap the content in a ChatCompletion to return.
        return new ChatCompletion(returnMessages)
        {
            RawRepresentation = response,
            CompletionId = response.Id,
            CreatedAt = response.Created,
            ModelId = response.Model,
            FinishReason = finishReason,
            Usage = usage,
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        Dictionary<int, FunctionCallInfo>? functionCallInfos = null;
        ChatRole? streamedRole = default;
        ChatFinishReason? finishReason = default;
        string? completionId = null;
        DateTimeOffset? createdAt = null;
        string? modelId = null;
        string? authorName = null;

        // Process each update as it arrives
        var updates = await _chatCompletionsClient.CompleteStreamingAsync(ToAzureAIOptions(chatMessages, options), cancellationToken).ConfigureAwait(false);
        await foreach (StreamingChatCompletionsUpdate chatCompletionUpdate in updates.ConfigureAwait(false))
        {
            // The role and finish reason may arrive during any update, but once they've arrived, the same value should be the same for all subsequent updates.
            streamedRole ??= chatCompletionUpdate.Role is global::Azure.AI.Inference.ChatRole role ? ToChatRole(role) : null;
            finishReason ??= chatCompletionUpdate.FinishReason is CompletionsFinishReason reason ? ToFinishReason(reason) : null;
            completionId ??= chatCompletionUpdate.Id;
            createdAt ??= chatCompletionUpdate.Created;
            modelId ??= chatCompletionUpdate.Model;
            authorName ??= chatCompletionUpdate.AuthorName;

            // Create the response content object.
            StreamingChatCompletionUpdate completionUpdate = new()
            {
                AuthorName = authorName,
                CompletionId = chatCompletionUpdate.Id,
                CreatedAt = chatCompletionUpdate.Created,
                FinishReason = finishReason,
                RawRepresentation = chatCompletionUpdate,
                Role = streamedRole,
            };

            // Transfer over content update items.
            if (chatCompletionUpdate.ContentUpdate is string update)
            {
                completionUpdate.Contents.Add(new TextContent(update)
                {
                    ModelId = modelId,
                });
            }

            // Transfer over tool call updates.
            if (chatCompletionUpdate.ToolCallUpdate is StreamingFunctionToolCallUpdate toolCallUpdate)
            {
                functionCallInfos ??= [];
                if (!functionCallInfos.TryGetValue(toolCallUpdate.ToolCallIndex, out FunctionCallInfo? existing))
                {
                    functionCallInfos[toolCallUpdate.ToolCallIndex] = existing = new();
                }

                existing.CallId ??= toolCallUpdate.Id;
                existing.Name ??= toolCallUpdate.Name;
                if (toolCallUpdate.ArgumentsUpdate is not null)
                {
                    _ = (existing.Arguments ??= new()).Append(toolCallUpdate.ArgumentsUpdate);
                }
            }

            // Now yield the item.
            yield return completionUpdate;
        }

        // TODO: Add usage as content when it's exposed by Azure.AI.Inference.

        // Now that we've received all updates, combine any for function calls into a single item to yield.
        if (functionCallInfos is not null)
        {
            var completionUpdate = new StreamingChatCompletionUpdate
            {
                AuthorName = authorName,
                CompletionId = completionId,
                CreatedAt = createdAt,
                FinishReason = finishReason,
                Role = streamedRole,
            };

            foreach (var entry in functionCallInfos)
            {
                FunctionCallInfo fci = entry.Value;
                if (!string.IsNullOrWhiteSpace(fci.Name))
                {
                    FunctionCallContent callContent = ParseCallContentFromJsonString(
                        fci.Arguments?.ToString() ?? string.Empty,
                        fci.CallId!,
                        fci.Name!);

                    callContent.ModelId = modelId;

                    completionUpdate.Contents.Add(callContent);
                }
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

    /// <summary>Converts an AzureAI role to an Extensions role.</summary>
    private static ChatRole ToChatRole(global::Azure.AI.Inference.ChatRole role) =>
        role.Equals(global::Azure.AI.Inference.ChatRole.System) ? ChatRole.System :
        role.Equals(global::Azure.AI.Inference.ChatRole.User) ? ChatRole.User :
        role.Equals(global::Azure.AI.Inference.ChatRole.Assistant) ? ChatRole.Assistant :
        role.Equals(global::Azure.AI.Inference.ChatRole.Tool) ? ChatRole.Tool :
        new ChatRole(role.ToString());

    /// <summary>Converts an AzureAI finish reason to an Extensions finish reason.</summary>
    private static ChatFinishReason? ToFinishReason(CompletionsFinishReason? finishReason) =>
        finishReason?.ToString() is not string s ? null :
        finishReason == CompletionsFinishReason.Stopped ? ChatFinishReason.Stop :
        finishReason == CompletionsFinishReason.TokenLimitReached ? ChatFinishReason.Length :
        finishReason == CompletionsFinishReason.ContentFiltered ? ChatFinishReason.ContentFilter :
        finishReason == CompletionsFinishReason.ToolCalls ? ChatFinishReason.ToolCalls :
        new(s);

    /// <summary>Converts an extensions options instance to an AzureAI options instance.</summary>
    private ChatCompletionsOptions ToAzureAIOptions(IList<ChatMessage> chatContents, ChatOptions? options)
    {
        ChatCompletionsOptions result = new(ToAzureAIInferenceChatMessages(chatContents))
        {
            Model = options?.ModelId ?? Metadata.ModelId ?? throw new InvalidOperationException("No model id was provided when either constructing the client or in the chat options.")
        };

        if (options is not null)
        {
            result.FrequencyPenalty = options.FrequencyPenalty;
            result.MaxTokens = options.MaxOutputTokens;
            result.NucleusSamplingFactor = options.TopP;
            result.PresencePenalty = options.PresencePenalty;
            result.Temperature = options.Temperature;

            if (options.StopSequences is { Count: > 0 } stopSequences)
            {
                foreach (string stopSequence in stopSequences)
                {
                    result.StopSequences.Add(stopSequence);
                }
            }

            // These properties are strongly-typed on ChatOptions but not on ChatCompletionsOptions.
            if (options.TopK is int topK)
            {
                result.AdditionalProperties["top_k"] = BinaryData.FromObjectAsJson(topK, ToolCallJsonSerializerOptions);
            }

            if (options.AdditionalProperties is { } props)
            {
                foreach (var prop in props)
                {
                    switch (prop.Key)
                    {
                        // These properties are strongly-typed on the ChatCompletionsOptions class but not on the ChatOptions class.
                        case nameof(result.Seed) when prop.Value is long seed:
                            result.Seed = seed;
                            break;

                        // Propagate everything else to the ChatCompletionOptions' AdditionalProperties.
                        default:
                            if (prop.Value is not null)
                            {
                                result.AdditionalProperties[prop.Key] = BinaryData.FromObjectAsJson(prop.Value, ToolCallJsonSerializerOptions);
                            }

                            break;
                    }
                }
            }

            if (options.Tools is { Count: > 0 } tools)
            {
                foreach (AITool tool in tools)
                {
                    if (tool is AIFunction af)
                    {
                        result.Tools.Add(ToAzureAIChatTool(af));
                    }
                }

                switch (options.ToolMode)
                {
                    case AutoChatToolMode:
                        result.ToolChoice = ChatCompletionsToolChoice.Auto;
                        break;

                    case RequiredChatToolMode required:
                        result.ToolChoice = required.RequiredFunctionName is null ?
                            ChatCompletionsToolChoice.Required :
                            new ChatCompletionsToolChoice(new FunctionDefinition(required.RequiredFunctionName));
                        break;
                }
            }

            if (options.ResponseFormat is ChatResponseFormatText)
            {
                result.ResponseFormat = new ChatCompletionsResponseFormatText();
            }
            else if (options.ResponseFormat is ChatResponseFormatJson)
            {
                result.ResponseFormat = new ChatCompletionsResponseFormatJSON();
            }
        }

        return result;
    }

    /// <summary>Converts an Extensions function to an AzureAI chat tool.</summary>
    private static ChatCompletionsFunctionToolDefinition ToAzureAIChatTool(AIFunction aiFunction)
    {
        BinaryData resultParameters = AzureAIChatToolJson.ZeroFunctionParametersSchema;

        var parameters = aiFunction.Metadata.Parameters;
        if (parameters is { Count: > 0 })
        {
            AzureAIChatToolJson tool = new();

            foreach (AIFunctionParameterMetadata parameter in parameters)
            {
                tool.Properties.Add(
                    parameter.Name,
                    parameter.Schema is JsonElement schema ? schema : _defaultParameterSchema);

                if (parameter.IsRequired)
                {
                    tool.Required.Add(parameter.Name);
                }
            }

            resultParameters = BinaryData.FromBytes(
                JsonSerializer.SerializeToUtf8Bytes(tool, JsonContext.Default.AzureAIChatToolJson));
        }

        return new()
        {
            Name = aiFunction.Metadata.Name,
            Description = aiFunction.Metadata.Description,
            Parameters = resultParameters,
        };
    }

    /// <summary>Used to create the JSON payload for an AzureAI chat tool description.</summary>
    private sealed class AzureAIChatToolJson
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

    /// <summary>Converts an Extensions chat message enumerable to an AzureAI chat message enumerable.</summary>
    private IEnumerable<ChatRequestMessage> ToAzureAIInferenceChatMessages(IEnumerable<ChatMessage> inputs)
    {
        // Maps all of the M.E.AI types to the corresponding AzureAI types.
        // Unrecognized content is ignored.

        foreach (ChatMessage input in inputs)
        {
            if (input.Role == ChatRole.System)
            {
                yield return new ChatRequestSystemMessage(input.Text);
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

                        yield return new ChatRequestToolMessage(result ?? string.Empty, resultContent.CallId);
                    }
                }
            }
            else if (input.Role == ChatRole.User)
            {
                yield return new ChatRequestUserMessage(input.Contents.Select(static (AIContent item) => item switch
                {
                    TextContent textContent => new ChatMessageTextContentItem(textContent.Text),
                    ImageContent imageContent => imageContent.Data is { IsEmpty: false } data ? new ChatMessageImageContentItem(BinaryData.FromBytes(data), imageContent.MediaType) :
                                                 imageContent.Uri is string uri ? new ChatMessageImageContentItem(new Uri(uri)) :
                                                 (ChatMessageContentItem?)null,
                    _ => null,
                }).Where(c => c is not null));
            }
            else if (input.Role == ChatRole.Assistant)
            {
                Dictionary<string, ChatCompletionsToolCall>? toolCalls = null;

                foreach (var content in input.Contents)
                {
                    if (content is FunctionCallContent callRequest && callRequest.CallId is not null && toolCalls?.ContainsKey(callRequest.CallId) is not true)
                    {
                        JsonSerializerOptions serializerOptions = ToolCallJsonSerializerOptions ?? JsonContext.Default.Options;
                        string jsonArguments = JsonSerializer.Serialize(callRequest.Arguments, serializerOptions.GetTypeInfo(typeof(IDictionary<string, object>)));
                        (toolCalls ??= []).Add(
                            callRequest.CallId,
                            new ChatCompletionsFunctionToolCall(
                                callRequest.CallId,
                                callRequest.Name,
                                jsonArguments));
                    }
                }

                ChatRequestAssistantMessage message = new();
                if (toolCalls is not null)
                {
                    foreach (var entry in toolCalls)
                    {
                        message.ToolCalls.Add(entry.Value);
                    }
                }
                else
                {
                    message.Content = input.Text;
                }

                yield return message;
            }
        }
    }

    private static FunctionCallContent ParseCallContentFromJsonString(string json, string callId, string name) =>
        FunctionCallContent.CreateFromParsedArguments(json, callId, name,
            argumentParser: static json => JsonSerializer.Deserialize(json, JsonContext.Default.IDictionaryStringObject),
            exceptionFilter: static ex => ex is JsonException);

    /// <summary>Source-generated JSON type information.</summary>
    [JsonSerializable(typeof(AzureAIChatToolJson))]
    [JsonSerializable(typeof(IDictionary<string, object?>))]
    [JsonSerializable(typeof(JsonElement))]
    private sealed partial class JsonContext : JsonSerializerContext;
}
