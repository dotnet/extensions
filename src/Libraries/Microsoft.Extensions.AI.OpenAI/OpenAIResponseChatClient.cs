// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI.Responses;

#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1108 // Block statements should not contain embedded comments

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IChatClient"/> for an <see cref="OpenAIResponseClient"/>.</summary>
internal sealed class OpenAIResponseChatClient : IChatClient
{
    /// <summary>Gets the default OpenAI endpoint.</summary>
    internal static Uri DefaultOpenAIEndpoint { get; } = new("https://api.openai.com/v1");

    /// <summary>A <see cref="ChatRole"/> for "developer".</summary>
    private static readonly ChatRole _chatRoleDeveloper = new("developer");

    /// <summary>Cached <see cref="BinaryData"/> for the string "none".</summary>
    private static readonly BinaryData _none = BinaryData.FromBytes("\"none\""u8.ToArray());

    /// <summary>Cached <see cref="BinaryData"/> for the string "auto".</summary>
    private static readonly BinaryData _auto = BinaryData.FromBytes("\"auto\""u8.ToArray());

    /// <summary>Cached <see cref="BinaryData"/> for the string "required".</summary>
    private static readonly BinaryData _required = BinaryData.FromBytes("\"required\""u8.ToArray());

    /// <summary>Metadata about the client.</summary>
    private readonly ChatClientMetadata _metadata;

    /// <summary>The underlying <see cref="OpenAIResponseClient" />.</summary>
    private readonly OpenAIResponseClient _responseClient;

    /// <summary>The <see cref="JsonSerializerOptions"/> use for any serialization activities related to tool call arguments and results.</summary>
    private JsonSerializerOptions _toolCallJsonSerializerOptions = AIJsonUtilities.DefaultOptions;

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

    /// <summary>Gets or sets <see cref="JsonSerializerOptions"/> to use for any serialization activities related to tool call arguments and results.</summary>
    public JsonSerializerOptions ToolCallJsonSerializerOptions
    {
        get => _toolCallJsonSerializerOptions;
        set => _toolCallJsonSerializerOptions = Throw.IfNull(value);
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
        var openAIResponseItems = ToOpenAIResponseItems(messages, ToolCallJsonSerializerOptions);
        var openAIOptions = ToOpenAIResponseCreationOptions(options);

        // Make the call to the OpenAIResponseClient.
        var openAIResponse = (await _responseClient.CreateResponseAsync(openAIResponseItems, openAIOptions, cancellationToken).ConfigureAwait(false)).Value;

        // Convert and return the results.
        ChatResponse response = new()
        {
            ResponseId = openAIResponse.Id,
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
                                static json => JsonSerializer.Deserialize(json.Span, OpenAIJsonContext.Default.IDictionaryStringObject)!));
                        break;
                }
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
        var openAIResponseItems = ToOpenAIResponseItems(messages, ToolCallJsonSerializerOptions);
        var openAIOptions = ToOpenAIResponseCreationOptions(options);

        // Make the call to the OpenAIResponseClient and process the streaming results.
        Dictionary<int, FunctionCallInfo>? functionCallInfos = null;
        DateTimeOffset? createdAt = null;
        string? responseId = null;
        string? modelId = null;
        ChatRole? role = null;
        ChatFinishReason? finishReason = null;
        UsageDetails? usage = null;
        await foreach (var streamingUpdate in _responseClient.CreateResponseStreamingAsync(openAIResponseItems, openAIOptions, cancellationToken).ConfigureAwait(false))
        {
            // Handle metadata updates about the overall response.
            if (streamingUpdate is StreamingResponseStatusUpdate statusUpdate)
            {
                createdAt ??= statusUpdate.Response.CreatedAt;
                responseId ??= statusUpdate.Response.Id;
                modelId ??= statusUpdate.Response.Model;
                finishReason ??= ToFinishReason(statusUpdate.Response?.IncompleteStatusDetails?.Reason);
                usage ??= ToUsageDetails(statusUpdate.Response);
                continue;
            }

            if (streamingUpdate is StreamingResponseItemUpdate itemUpdate)
            {
                // Handle metadata updates about the message.
                if (itemUpdate.Item is MessageResponseItem messageItem)
                {
                    role ??= ToChatRole(messageItem.Role);
                    continue;
                }

                // Handle function call updates (name/id). Arguments come as part of content.
                if (itemUpdate.Item is FunctionCallResponseItem functionCallItem)
                {
                    functionCallInfos ??= [];
                    if (!functionCallInfos.TryGetValue(itemUpdate.ItemIndex, out FunctionCallInfo? callInfo))
                    {
                        functionCallInfos[itemUpdate.ItemIndex] = callInfo = new();
                    }

                    callInfo.CallId = functionCallItem.CallId;
                    callInfo.Name = functionCallItem.FunctionName;
                    continue;
                }
            }

            // Handle content updates.
            if (streamingUpdate is StreamingResponseContentPartDeltaUpdate contentUpdate)
            {
                // Update our knowledge of function call requests.
                if (contentUpdate.FunctionArguments is string argsUpdate)
                {
                    functionCallInfos ??= [];
                    if (!functionCallInfos.TryGetValue(contentUpdate.ItemIndex, out FunctionCallInfo? callInfo))
                    {
                        functionCallInfos[contentUpdate.ItemIndex] = callInfo = new();
                    }

                    _ = (callInfo.Arguments ??= new()).Append(argsUpdate);
                }

                // If there's any text content, return it.
                if (!string.IsNullOrEmpty(contentUpdate.Text))
                {
                    yield return new(role, contentUpdate.Text)
                    {
                        CreatedAt = createdAt,
                        ModelId = modelId,
                        RawRepresentation = streamingUpdate,
                        ResponseId = responseId,
                    };
                }

                continue;
            }
        }

        // Now that we've received all updates and yielded all content,
        // yield a final update with any remaining information.
        ChatResponseUpdate update = new()
        {
            ResponseId = responseId,
            CreatedAt = createdAt,
            FinishReason = finishReason ?? (functionCallInfos is not null ? ChatFinishReason.ToolCalls : ChatFinishReason.Stop),
            ModelId = modelId,
            Role = role,
        };

        if (usage is not null)
        {
            update.Contents.Add(new UsageContent(usage));
        }

        if (functionCallInfos is not null)
        {
            foreach (var entry in functionCallInfos)
            {
                FunctionCallInfo fci = entry.Value;
                if (!string.IsNullOrWhiteSpace(fci.Name))
                {
                    update.Contents.Add(
                        FunctionCallContent.CreateFromParsedArguments(
                            fci.Arguments?.ToString() ?? string.Empty,
                            fci.CallId ?? string.Empty,
                            fci.Name!,
                            static json => JsonSerializer.Deserialize(json, OpenAIJsonContext.Default.IDictionaryStringObject)!));
                }
            }
        }

        yield return update;
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IChatClient interface.
    }

    /// <summary>Creates a <see cref="ChatRole"/> from a <see cref="MessageRole"/>.</summary>
    private static ChatRole ToChatRole(MessageRole? role) =>
        role == MessageRole.System ? ChatRole.System :
        role == MessageRole.Developer ? _chatRoleDeveloper :
        role == MessageRole.User ? ChatRole.User :
        ChatRole.Assistant;

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
            result.PreviousResponseId = options.ChatThreadId;
            result.TopP = options.TopP;
            result.Temperature = options.Temperature;

            // Handle loosely-typed properties from AdditionalProperties.
            if (options.AdditionalProperties is { Count: > 0 } additionalProperties)
            {
                if (additionalProperties.TryGetValue(nameof(result.AllowParallelToolCalls), out bool allowParallelToolCalls))
                {
                    result.AllowParallelToolCalls = allowParallelToolCalls;
                }

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
                            var oaitool = JsonSerializer.Deserialize(af.JsonSchema, OpenAIJsonContext.Default.OpenAIChatToolJson)!;
                            var functionParameters = BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(oaitool, OpenAIJsonContext.Default.OpenAIChatToolJson));
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
                        result.ToolChoice = _none;
                        break;

                    case AutoChatToolMode:
                    case null:
                        result.ToolChoice = _auto;
                        break;

                    case RequiredChatToolMode required:
                        result.ToolChoice = required.RequiredFunctionName is not null ?
                            BinaryData.FromString($$"""{"type":"function","name":"{{required.RequiredFunctionName}}"}""") :
                            _required;
                        break;
                }
            }

            // Handle response format.
            if (options.ResponseFormat is ChatResponseFormatText)
            {
                result.TextOptions.ResponseFormat = ResponseTextFormat.CreateTextFormat();
            }
            else if (options.ResponseFormat is ChatResponseFormatJson jsonFormat)
            {
                result.TextOptions.ResponseFormat = jsonFormat.Schema is { } jsonSchema ?
                    ResponseTextFormat.CreateJsonSchemaFormat(
                        jsonFormat.SchemaName ?? "json_schema",
                        BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(jsonSchema, OpenAIJsonContext.Default.JsonElement)),
                        jsonFormat.SchemaDescription,
                        jsonSchemaIsStrict: true) :
                    ResponseTextFormat.CreateJsonObjectFormat();
            }
        }

        return result;
    }

    /// <summary>Convert a sequence of <see cref="ChatMessage"/>s to <see cref="ResponseItem"/>s.</summary>
    private static IEnumerable<ResponseItem> ToOpenAIResponseItems(
        IEnumerable<ChatMessage> inputs, JsonSerializerOptions options)
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
                                    result = JsonSerializer.Serialize(resultContent.Result, options.GetTypeInfo(typeof(object)));
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
                            yield return ResponseItem.CreateAssistantMessageItem(
                                "msg_ignored",
                                textContent.Text);
                            break;

                        case FunctionCallContent callContent:
                            yield return ResponseItem.CreateFunctionCall(
                                "msg_ignored",
                                callContent.CallId,
                                callContent.Name,
                                BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(
                                    callContent.Arguments,
                                    options.GetTypeInfo(typeof(IDictionary<string, object?>)))));
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

    /// <summary>POCO representing function calling info.</summary>
    /// <remarks>Used to concatenation information for a single function call from across multiple streaming updates.</remarks>
    private sealed class FunctionCallInfo
    {
        public string? CallId;
        public string? Name;
        public StringBuilder? Arguments;
    }
}
