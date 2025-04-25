// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI.Responses;
using static Microsoft.Extensions.AI.OpenAIChatClient;

#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable S3604 // Member initializer values should not be redundant

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IChatClient"/> for an <see cref="OpenAIResponseClient"/>.</summary>
internal sealed partial class OpenAIResponseChatClient : IChatClient
{
    /// <summary>Gets the default OpenAI endpoint.</summary>
    private static Uri DefaultOpenAIEndpoint { get; } = new("https://api.openai.com/v1");

    /// <summary>A <see cref="ChatRole"/> for "developer".</summary>
    private static readonly ChatRole _chatRoleDeveloper = new("developer");

    /// <summary>Metadata about the client.</summary>
    private readonly ChatClientMetadata _metadata;

    /// <summary>The underlying <see cref="OpenAIResponseClient" />.</summary>
    private readonly OpenAIResponseClient _responseClient;

    /// <summary>Initializes a new instance of the <see cref="OpenAIResponseChatClient"/> class for the specified <see cref="OpenAIResponseClient"/>.</summary>
    /// <param name="responseClient">The underlying client.</param>
    /// <exception cref="ArgumentNullException"><paramref name="responseClient"/> is <see langword="null"/>.</exception>
    public OpenAIResponseChatClient(OpenAIResponseClient responseClient)
    {
        _ = Throw.IfNull(responseClient);

        _responseClient = responseClient;

        // https://github.com/openai/openai-dotnet/issues/215
        // The endpoint and model aren't currently exposed, so use reflection to get at them, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        Uri providerUrl = typeof(OpenAIResponseClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(responseClient) as Uri ?? DefaultOpenAIEndpoint;
        string? model = typeof(OpenAIResponseClient).GetField("_model", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(responseClient) as string;

        _metadata = new("openai", providerUrl, model);
    }

    /// <inheritdoc />
    object? IChatClient.GetService(Type serviceType, object? serviceKey)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(ChatClientMetadata) ? _metadata :
            serviceType == typeof(OpenAIResponseClient) ? _responseClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        // Convert the inputs into what OpenAIResponseClient expects.
        var openAIResponseItems = ToOpenAIResponseItems(messages);
        var openAIOptions = ToOpenAIResponseCreationOptions(options);

        // Make the call to the OpenAIResponseClient.
        var openAIResponse = (await _responseClient.CreateResponseAsync(openAIResponseItems, openAIOptions, cancellationToken).ConfigureAwait(false)).Value;

        // Convert and return the results.
        ChatResponse response = new()
        {
            ResponseId = openAIResponse.Id,
            ConversationId = openAIResponse.Id,
            CreatedAt = openAIResponse.CreatedAt,
            FinishReason = ToFinishReason(openAIResponse.IncompleteStatusDetails?.Reason),
            Messages = [new(ChatRole.Assistant, [])],
            ModelId = openAIResponse.Model,
            Usage = ToUsageDetails(openAIResponse),
        };

        if (!string.IsNullOrEmpty(openAIResponse.EndUserId))
        {
            (response.AdditionalProperties ??= [])[nameof(openAIResponse.EndUserId)] = openAIResponse.EndUserId;
        }

        if (openAIResponse.Error is not null)
        {
            (response.AdditionalProperties ??= [])[nameof(openAIResponse.Error)] = openAIResponse.Error;
        }

        if (openAIResponse.OutputItems is not null)
        {
            ChatMessage message = response.Messages[0];
            Debug.Assert(message.Contents is List<AIContent>, "Expected a List<AIContent> for message contents.");

            foreach (ResponseItem outputItem in openAIResponse.OutputItems)
            {
                switch (outputItem)
                {
                    case MessageResponseItem messageItem:
                        message.MessageId = messageItem.Id;
                        message.RawRepresentation = messageItem;
                        message.Role = ToChatRole(messageItem.Role);
                        (message.AdditionalProperties ??= []).Add(nameof(messageItem.Id), messageItem.Id);
                        ((List<AIContent>)message.Contents).AddRange(ToAIContents(messageItem.Content));
                        break;

                    case FunctionCallResponseItem functionCall:
                        response.FinishReason ??= ChatFinishReason.ToolCalls;
                        message.Contents.Add(
                            FunctionCallContent.CreateFromParsedArguments(
                                functionCall.FunctionArguments.ToMemory(),
                                functionCall.CallId,
                                functionCall.FunctionName,
                                static json => JsonSerializer.Deserialize(json.Span, ResponseClientJsonContext.Default.IDictionaryStringObject)!));
                        break;
                }
            }

            if (openAIResponse.Error is { } error)
            {
                message.Contents.Add(new ErrorContent(error.Message) { ErrorCode = error.Code });
            }
        }

        return response;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        // Convert the inputs into what OpenAIResponseClient expects.
        var openAIResponseItems = ToOpenAIResponseItems(messages);
        var openAIOptions = ToOpenAIResponseCreationOptions(options);

        // Make the call to the OpenAIResponseClient and process the streaming results.
        DateTimeOffset? createdAt = null;
        string? responseId = null;
        string? modelId = null;
        string? lastMessageId = null;
        ChatRole? lastRole = null;
        Dictionary<int, MessageResponseItem> outputIndexToMessages = [];
        Dictionary<int, FunctionCallInfo>? functionCallInfos = null;
        await foreach (var streamingUpdate in _responseClient.CreateResponseStreamingAsync(openAIResponseItems, openAIOptions, cancellationToken).ConfigureAwait(false))
        {
            switch (streamingUpdate)
            {
                case StreamingResponseCreatedUpdate createdUpdate:
                    createdAt = createdUpdate.Response.CreatedAt;
                    responseId = createdUpdate.Response.Id;
                    modelId = createdUpdate.Response.Model;
                    break;

                case StreamingResponseCompletedUpdate completedUpdate:
                    yield return new()
                    {
                        Contents = ToUsageDetails(completedUpdate.Response) is { } usage ? [new UsageContent(usage)] : [],
                        CreatedAt = createdAt,
                        ResponseId = responseId,
                        ConversationId = responseId,
                        FinishReason =
                            ToFinishReason(completedUpdate.Response?.IncompleteStatusDetails?.Reason) ??
                            (functionCallInfos is not null ? ChatFinishReason.ToolCalls : ChatFinishReason.Stop),
                        MessageId = lastMessageId,
                        ModelId = modelId,
                        Role = lastRole,
                    };
                    break;

                case StreamingResponseOutputItemAddedUpdate outputItemAddedUpdate:
                    switch (outputItemAddedUpdate.Item)
                    {
                        case MessageResponseItem mri:
                            outputIndexToMessages[outputItemAddedUpdate.OutputIndex] = mri;
                            break;

                        case FunctionCallResponseItem fcri:
                            (functionCallInfos ??= [])[outputItemAddedUpdate.OutputIndex] = new(fcri);
                            break;
                    }

                    break;

                case StreamingResponseOutputItemDoneUpdate outputItemDoneUpdate:
                    _ = outputIndexToMessages.Remove(outputItemDoneUpdate.OutputIndex);
                    break;

                case StreamingResponseOutputTextDeltaUpdate outputTextDeltaUpdate:
                    _ = outputIndexToMessages.TryGetValue(outputTextDeltaUpdate.OutputIndex, out MessageResponseItem? messageItem);
                    lastMessageId = messageItem?.Id;
                    lastRole = ToChatRole(messageItem?.Role);
                    yield return new ChatResponseUpdate(lastRole, outputTextDeltaUpdate.Delta)
                    {
                        CreatedAt = createdAt,
                        MessageId = lastMessageId,
                        ModelId = modelId,
                        ResponseId = responseId,
                        ConversationId = responseId,
                    };
                    break;

                case StreamingResponseFunctionCallArgumentsDeltaUpdate functionCallArgumentsDeltaUpdate:
                {
                    if (functionCallInfos?.TryGetValue(functionCallArgumentsDeltaUpdate.OutputIndex, out FunctionCallInfo? callInfo) is true)
                    {
                        _ = (callInfo.Arguments ??= new()).Append(functionCallArgumentsDeltaUpdate.Delta);
                    }

                    break;
                }

                case StreamingResponseFunctionCallArgumentsDoneUpdate functionCallOutputDoneUpdate:
                {
                    if (functionCallInfos?.TryGetValue(functionCallOutputDoneUpdate.OutputIndex, out FunctionCallInfo? callInfo) is true)
                    {
                        _ = functionCallInfos.Remove(functionCallOutputDoneUpdate.OutputIndex);

                        var fci = FunctionCallContent.CreateFromParsedArguments(
                            callInfo.Arguments?.ToString() ?? string.Empty,
                            callInfo.ResponseItem.CallId,
                            callInfo.ResponseItem.FunctionName,
                            static json => JsonSerializer.Deserialize(json, ResponseClientJsonContext.Default.IDictionaryStringObject)!);

                        lastMessageId = callInfo.ResponseItem.Id;
                        lastRole = ChatRole.Assistant;
                        yield return new ChatResponseUpdate(lastRole, [fci])
                        {
                            CreatedAt = createdAt,
                            MessageId = lastMessageId,
                            ModelId = modelId,
                            ResponseId = responseId,
                            ConversationId = responseId,
                        };
                    }

                    break;
                }

                case StreamingResponseErrorUpdate errorUpdate:
                    yield return new ChatResponseUpdate
                    {
                        CreatedAt = createdAt,
                        MessageId = lastMessageId,
                        ModelId = modelId,
                        ResponseId = responseId,
                        ConversationId = responseId,
                        Contents =
                        [
                            new ErrorContent(errorUpdate.Message)
                            {
                                ErrorCode = errorUpdate.Code,
                                Details = errorUpdate.Param,
                            }
                        ],
                    };
                    break;
            }
        }
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IChatClient interface.
    }

    /// <summary>Creates a <see cref="ChatRole"/> from a <see cref="MessageRole"/>.</summary>
    private static ChatRole ToChatRole(MessageRole? role) =>
        role switch
        {
            MessageRole.System => ChatRole.System,
            MessageRole.Developer => _chatRoleDeveloper,
            MessageRole.User => ChatRole.User,
            _ => ChatRole.Assistant,
        };

    /// <summary>Creates a <see cref="ChatFinishReason"/> from a <see cref="ResponseIncompleteStatusReason"/>.</summary>
    private static ChatFinishReason? ToFinishReason(ResponseIncompleteStatusReason? statusReason) =>
        statusReason == ResponseIncompleteStatusReason.ContentFilter ? ChatFinishReason.ContentFilter :
        statusReason == ResponseIncompleteStatusReason.MaxOutputTokens ? ChatFinishReason.Length :
        null;

    /// <summary>Converts a <see cref="ChatOptions"/> to a <see cref="ResponseCreationOptions"/>.</summary>
    private static ResponseCreationOptions ToOpenAIResponseCreationOptions(ChatOptions? options)
    {
        ResponseCreationOptions result = new();

        if (options is not null)
        {
            // Handle strongly-typed properties.
            result.MaxOutputTokenCount = options.MaxOutputTokens;
            result.PreviousResponseId = options.ConversationId;
            result.TopP = options.TopP;
            result.Temperature = options.Temperature;
            result.ParallelToolCallsEnabled = options.AllowMultipleToolCalls;

            // Handle loosely-typed properties from AdditionalProperties.
            if (options.AdditionalProperties is { Count: > 0 } additionalProperties)
            {
                if (additionalProperties.TryGetValue(nameof(result.EndUserId), out string? endUserId))
                {
                    result.EndUserId = endUserId;
                }

                if (additionalProperties.TryGetValue(nameof(result.Instructions), out string? instructions))
                {
                    result.Instructions = instructions;
                }

                if (additionalProperties.TryGetValue(nameof(result.Metadata), out IDictionary<string, string>? metadata))
                {
                    foreach (KeyValuePair<string, string> kvp in metadata)
                    {
                        result.Metadata[kvp.Key] = kvp.Value;
                    }
                }

                if (additionalProperties.TryGetValue(nameof(result.ReasoningOptions), out ResponseReasoningOptions? reasoningOptions))
                {
                    result.ReasoningOptions = reasoningOptions;
                }

                if (additionalProperties.TryGetValue(nameof(result.StoredOutputEnabled), out bool storeOutputEnabled))
                {
                    result.StoredOutputEnabled = storeOutputEnabled;
                }

                if (additionalProperties.TryGetValue(nameof(result.TruncationMode), out ResponseTruncationMode truncationMode))
                {
                    result.TruncationMode = truncationMode;
                }
            }

            // Populate tools if there are any.
            if (options.Tools is { Count: > 0 } tools)
            {
                foreach (AITool tool in tools)
                {
                    switch (tool)
                    {
                        case AIFunction af:
                            var oaitool = JsonSerializer.Deserialize(af.JsonSchema, ResponseClientJsonContext.Default.ResponseToolJson)!;
                            var functionParameters = BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(oaitool, ResponseClientJsonContext.Default.ResponseToolJson));
                            result.Tools.Add(ResponseTool.CreateFunctionTool(af.Name, af.Description, functionParameters));
                            break;

                        case HostedWebSearchTool:
                            WebSearchToolLocation? location = null;
                            if (tool.AdditionalProperties.TryGetValue(nameof(WebSearchToolLocation), out object? objLocation))
                            {
                                location = objLocation as WebSearchToolLocation;
                            }

                            WebSearchToolContextSize? size = null;
                            if (tool.AdditionalProperties.TryGetValue(nameof(WebSearchToolContextSize), out object? objSize) &&
                                objSize is WebSearchToolContextSize)
                            {
                                size = (WebSearchToolContextSize)objSize;
                            }

                            result.Tools.Add(ResponseTool.CreateWebSearchTool(location, size));
                            break;
                    }
                }

                switch (options.ToolMode)
                {
                    case NoneChatToolMode:
                        result.ToolChoice = ResponseToolChoice.CreateNoneChoice();
                        break;

                    case AutoChatToolMode:
                    case null:
                        result.ToolChoice = ResponseToolChoice.CreateAutoChoice();
                        break;

                    case RequiredChatToolMode required:
                        result.ToolChoice = required.RequiredFunctionName is not null ?
                            ResponseToolChoice.CreateFunctionChoice(required.RequiredFunctionName) :
                            ResponseToolChoice.CreateRequiredChoice();
                        break;
                }
            }

            // Handle response format.
            if (options.ResponseFormat is ChatResponseFormatText)
            {
                result.TextOptions = new()
                {
                    TextFormat = ResponseTextFormat.CreateTextFormat()
                };
            }
            else if (options.ResponseFormat is ChatResponseFormatJson jsonFormat)
            {
                result.TextOptions = new()
                {
                    TextFormat = jsonFormat.Schema is { } jsonSchema ?
                        ResponseTextFormat.CreateJsonSchemaFormat(
                            jsonFormat.SchemaName ?? "json_schema",
                            BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(jsonSchema, ResponseClientJsonContext.Default.JsonElement)),
                            jsonFormat.SchemaDescription) :
                        ResponseTextFormat.CreateJsonObjectFormat(),
                };
            }
        }

        return result;
    }

    /// <summary>Convert a sequence of <see cref="ChatMessage"/>s to <see cref="ResponseItem"/>s.</summary>
    private static IEnumerable<ResponseItem> ToOpenAIResponseItems(
        IEnumerable<ChatMessage> inputs)
    {
        foreach (ChatMessage input in inputs)
        {
            if (input.Role == ChatRole.System ||
                input.Role == _chatRoleDeveloper)
            {
                string text = input.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    yield return input.Role == ChatRole.System ?
                        ResponseItem.CreateSystemMessageItem(text) :
                        ResponseItem.CreateDeveloperMessageItem(text);
                }

                continue;
            }

            if (input.Role == ChatRole.User)
            {
                yield return ResponseItem.CreateUserMessageItem(ToOpenAIResponsesContent(input.Contents));
                continue;
            }

            if (input.Role == ChatRole.Tool)
            {
                foreach (AIContent item in input.Contents)
                {
                    switch (item)
                    {
                        case FunctionResultContent resultContent:
                            string? result = resultContent.Result as string;
                            if (result is null && resultContent.Result is not null)
                            {
                                try
                                {
                                    result = JsonSerializer.Serialize(resultContent.Result, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(object)));
                                }
                                catch (NotSupportedException)
                                {
                                    // If the type can't be serialized, skip it.
                                }
                            }

                            yield return ResponseItem.CreateFunctionCallOutputItem(resultContent.CallId, result ?? string.Empty);
                            break;
                    }
                }

                continue;
            }

            if (input.Role == ChatRole.Assistant)
            {
                foreach (AIContent item in input.Contents)
                {
                    switch (item)
                    {
                        case TextContent textContent:
                            yield return ResponseItem.CreateAssistantMessageItem(textContent.Text);
                            break;

                        case FunctionCallContent callContent:
                            yield return ResponseItem.CreateFunctionCallItem(
                                callContent.CallId,
                                callContent.Name,
                                BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(
                                    callContent.Arguments,
                                    AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(IDictionary<string, object?>)))));
                            break;
                    }
                }

                continue;
            }
        }
    }

    /// <summary>Extract usage details from an <see cref="OpenAIResponse"/>.</summary>
    private static UsageDetails? ToUsageDetails(OpenAIResponse? openAIResponse)
    {
        UsageDetails? ud = null;
        if (openAIResponse?.Usage is { } usage)
        {
            ud = new()
            {
                InputTokenCount = usage.InputTokenCount,
                OutputTokenCount = usage.OutputTokenCount,
                TotalTokenCount = usage.TotalTokenCount,
            };

            if (usage.OutputTokenDetails is { } outputDetails)
            {
                ud.AdditionalCounts ??= [];

                const string OutputDetails = nameof(usage.OutputTokenDetails);
                ud.AdditionalCounts.Add($"{OutputDetails}.{nameof(outputDetails.ReasoningTokenCount)}", outputDetails.ReasoningTokenCount);
            }
        }

        return ud;
    }

    /// <summary>Convert a sequence of <see cref="ResponseContentPart"/>s to a list of <see cref="AIContent"/>.</summary>
    private static List<AIContent> ToAIContents(IEnumerable<ResponseContentPart> contents)
    {
        List<AIContent> results = [];

        foreach (ResponseContentPart part in contents)
        {
            if (part.Kind == ResponseContentPartKind.OutputText)
            {
                results.Add(new TextContent(part.Text));
            }
        }

        return results;
    }

    /// <summary>Convert a list of <see cref="AIContent"/>s to a list of <see cref="ResponseContentPart"/>.</summary>
    private static List<ResponseContentPart> ToOpenAIResponsesContent(IList<AIContent> contents)
    {
        List<ResponseContentPart> parts = [];
        foreach (var content in contents)
        {
            switch (content)
            {
                case TextContent textContent:
                    parts.Add(ResponseContentPart.CreateInputTextPart(textContent.Text));
                    break;

                case UriContent uriContent when uriContent.HasTopLevelMediaType("image"):
                    parts.Add(ResponseContentPart.CreateInputImagePart(uriContent.Uri));
                    break;

                case DataContent dataContent when dataContent.HasTopLevelMediaType("image"):
                    parts.Add(ResponseContentPart.CreateInputImagePart(BinaryData.FromBytes(dataContent.Data), dataContent.MediaType));
                    break;
            }
        }

        if (parts.Count == 0)
        {
            parts.Add(ResponseContentPart.CreateInputTextPart(string.Empty));
        }

        return parts;
    }

    /// <summary>Used to create the JSON payload for an OpenAI chat tool description.</summary>
    private sealed class ResponseToolJson
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";

        [JsonPropertyName("required")]
        public HashSet<string> Required { get; set; } = [];

        [JsonPropertyName("properties")]
        public Dictionary<string, JsonElement> Properties { get; set; } = [];

        [JsonPropertyName("additionalProperties")]
        public bool AdditionalProperties { get; set; }
    }

    /// <summary>POCO representing function calling info.</summary>
    /// <remarks>Used to concatenation information for a single function call from across multiple streaming updates.</remarks>
    private sealed class FunctionCallInfo(FunctionCallResponseItem item)
    {
        public readonly FunctionCallResponseItem ResponseItem = item;
        public StringBuilder? Arguments;
    }

    /// <summary>Source-generated JSON type information.</summary>
    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
        UseStringEnumConverter = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true)]
    [JsonSerializable(typeof(ResponseToolJson))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(IDictionary<string, object?>))]
    [JsonSerializable(typeof(string[]))]
    private sealed partial class ResponseClientJsonContext : JsonSerializerContext;
}
