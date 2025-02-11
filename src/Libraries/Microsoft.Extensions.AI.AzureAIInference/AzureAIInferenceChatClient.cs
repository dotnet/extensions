// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.Inference;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S1135 // Track uses of "TODO" tags
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IChatClient"/> for an Azure AI Inference <see cref="ChatCompletionsClient"/>.</summary>
public sealed class AzureAIInferenceChatClient : IChatClient
{
    /// <summary>Metadata about the client.</summary>
    private readonly ChatClientMetadata _metadata;

    /// <summary>The underlying <see cref="ChatCompletionsClient" />.</summary>
    private readonly ChatCompletionsClient _chatCompletionsClient;

    /// <summary>The <see cref="JsonSerializerOptions"/> use for any serialization activities related to tool call arguments and results.</summary>
    private JsonSerializerOptions _toolCallJsonSerializerOptions = AIJsonUtilities.DefaultOptions;

    /// <summary>Initializes a new instance of the <see cref="AzureAIInferenceChatClient"/> class for the specified <see cref="ChatCompletionsClient"/>.</summary>
    /// <param name="chatCompletionsClient">The underlying client.</param>
    /// <param name="modelId">The ID of the model to use. If null, it can be provided per request via <see cref="ChatOptions.ModelId"/>.</param>
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

        _metadata = new("az.ai.inference", providerUrl, modelId);
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
            serviceType == typeof(ChatCompletionsClient) ? _chatCompletionsClient :
            serviceType == typeof(ChatClientMetadata) ? _metadata :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
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
        ChatMessage message = new()
        {
            RawRepresentation = response,
            Role = ToChatRole(response.Role),
        };

        if (response.Content is string content)
        {
            message.Text = content;
        }

        if (response.ToolCalls is { Count: > 0 } toolCalls)
        {
            foreach (var toolCall in toolCalls)
            {
                if (toolCall is ChatCompletionsToolCall ftc && !string.IsNullOrWhiteSpace(ftc.Name))
                {
                    FunctionCallContent callContent = ParseCallContentFromJsonString(ftc.Arguments, toolCall.Id, ftc.Name);
                    callContent.RawRepresentation = toolCall;

                    message.Contents.Add(callContent);
                }
            }
        }

        returnMessages.Add(message);

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

        // Wrap the content in a ChatResponse to return.
        return new ChatResponse(returnMessages)
        {
            CreatedAt = response.Created,
            ModelId = response.Model,
            FinishReason = ToFinishReason(response.FinishReason),
            RawRepresentation = response,
            ResponseId = response.Id,
            Usage = usage,
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        Dictionary<string, FunctionCallInfo>? functionCallInfos = null;
        ChatRole? streamedRole = default;
        ChatFinishReason? finishReason = default;
        string? responseId = null;
        DateTimeOffset? createdAt = null;
        string? modelId = null;
        string lastCallId = string.Empty;

        // Process each update as it arrives
        var updates = await _chatCompletionsClient.CompleteStreamingAsync(ToAzureAIOptions(chatMessages, options), cancellationToken).ConfigureAwait(false);
        await foreach (StreamingChatCompletionsUpdate chatCompletionUpdate in updates.ConfigureAwait(false))
        {
            // The role and finish reason may arrive during any update, but once they've arrived, the same value should be the same for all subsequent updates.
            streamedRole ??= chatCompletionUpdate.Role is global::Azure.AI.Inference.ChatRole role ? ToChatRole(role) : null;
            finishReason ??= chatCompletionUpdate.FinishReason is CompletionsFinishReason reason ? ToFinishReason(reason) : null;
            responseId ??= chatCompletionUpdate.Id;
            createdAt ??= chatCompletionUpdate.Created;
            modelId ??= chatCompletionUpdate.Model;

            // Create the response content object.
            ChatResponseUpdate responseUpdate = new()
            {
                CreatedAt = chatCompletionUpdate.Created,
                FinishReason = finishReason,
                ModelId = modelId,
                RawRepresentation = chatCompletionUpdate,
                ResponseId = chatCompletionUpdate.Id,
                Role = streamedRole,
            };

            // Transfer over content update items.
            if (chatCompletionUpdate.ContentUpdate is string update)
            {
                responseUpdate.Contents.Add(new TextContent(update));
            }

            // Transfer over tool call updates.
            if (chatCompletionUpdate.ToolCallUpdate is { } toolCallUpdate)
            {
                // TODO https://github.com/Azure/azure-sdk-for-net/issues/46830: Azure.AI.Inference
                // has removed the Index property from ToolCallUpdate. It's now impossible via the
                // exposed APIs to correctly handle multiple parallel tool calls, as the CallId is
                // often null for anything other than the first update for a given call, and Index
                // isn't available to correlate which updates are for which call. This is a temporary
                // workaround to at least make a single tool call work and also make work multiple
                // tool calls when their updates aren't interleaved.
                if (toolCallUpdate.Id is not null)
                {
                    lastCallId = toolCallUpdate.Id;
                }

                functionCallInfos ??= [];
                if (!functionCallInfos.TryGetValue(lastCallId, out FunctionCallInfo? existing))
                {
                    functionCallInfos[lastCallId] = existing = new();
                }

                existing.Name ??= toolCallUpdate.Function.Name;
                if (toolCallUpdate.Function.Arguments is { } arguments)
                {
                    _ = (existing.Arguments ??= new()).Append(arguments);
                }
            }

            if (chatCompletionUpdate.Usage is { } usage)
            {
                responseUpdate.Contents.Add(new UsageContent(new()
                {
                    InputTokenCount = usage.PromptTokens,
                    OutputTokenCount = usage.CompletionTokens,
                    TotalTokenCount = usage.TotalTokens,
                }));
            }

            // Now yield the item.
            yield return responseUpdate;
        }

        // Now that we've received all updates, combine any for function calls into a single item to yield.
        if (functionCallInfos is not null)
        {
            var responseUpdate = new ChatResponseUpdate
            {
                CreatedAt = createdAt,
                FinishReason = finishReason,
                ModelId = modelId,
                ResponseId = responseId,
                Role = streamedRole,
            };

            foreach (var entry in functionCallInfos)
            {
                FunctionCallInfo fci = entry.Value;
                if (!string.IsNullOrWhiteSpace(fci.Name))
                {
                    FunctionCallContent callContent = ParseCallContentFromJsonString(
                        fci.Arguments?.ToString() ?? string.Empty,
                        entry.Key,
                        fci.Name!);
                    responseUpdate.Contents.Add(callContent);
                }
            }

            yield return responseUpdate;
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
            Model = options?.ModelId ?? _metadata.ModelId ?? throw new InvalidOperationException("No model id was provided when either constructing the client or in the chat options.")
        };

        if (options is not null)
        {
            result.FrequencyPenalty = options.FrequencyPenalty;
            result.MaxTokens = options.MaxOutputTokens;
            result.NucleusSamplingFactor = options.TopP;
            result.PresencePenalty = options.PresencePenalty;
            result.Temperature = options.Temperature;
            result.Seed = options.Seed;

            if (options.StopSequences is { Count: > 0 } stopSequences)
            {
                foreach (string stopSequence in stopSequences)
                {
                    result.StopSequences.Add(stopSequence);
                }
            }

            // These properties are strongly typed on ChatOptions but not on ChatCompletionsOptions.
            if (options.TopK is int topK)
            {
                result.AdditionalProperties["top_k"] = new BinaryData(JsonSerializer.SerializeToUtf8Bytes(topK, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(int))));
            }

            if (options.AdditionalProperties is { } props)
            {
                foreach (var prop in props)
                {
                    switch (prop.Key)
                    {
                        // Propagate everything else to the ChatCompletionsOptions' AdditionalProperties.
                        default:
                            if (prop.Value is not null)
                            {
                                byte[] data = JsonSerializer.SerializeToUtf8Bytes(prop.Value, ToolCallJsonSerializerOptions.GetTypeInfo(typeof(object)));
                                result.AdditionalProperties[prop.Key] = new BinaryData(data);
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
                    case NoneChatToolMode:
                        result.ToolChoice = ChatCompletionsToolChoice.None;
                        break;

                    case AutoChatToolMode:
                    case null:
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
    private static ChatCompletionsToolDefinition ToAzureAIChatTool(AIFunction aiFunction)
    {
        // Map to an intermediate model so that redundant properties are skipped.
        var tool = JsonSerializer.Deserialize(aiFunction.Metadata.Schema, JsonContext.Default.AzureAIChatToolJson)!;
        var functionParameters = BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(tool, JsonContext.Default.AzureAIChatToolJson));
        return new(new FunctionDefinition(aiFunction.Metadata.Name)
        {
            Description = aiFunction.Metadata.Description,
            Parameters = functionParameters,
        });
    }

    /// <summary>Converts an Extensions chat message enumerable to an AzureAI chat message enumerable.</summary>
    private IEnumerable<ChatRequestMessage> ToAzureAIInferenceChatMessages(IList<ChatMessage> inputs)
    {
        // Maps all of the M.E.AI types to the corresponding AzureAI types.
        // Unrecognized or non-processable content is ignored.

        foreach (ChatMessage input in inputs)
        {
            if (input.Role == ChatRole.System)
            {
                yield return new ChatRequestSystemMessage(input.Text ?? string.Empty);
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
                            try
                            {
                                result = JsonSerializer.Serialize(resultContent.Result, ToolCallJsonSerializerOptions.GetTypeInfo(typeof(object)));
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
                yield return input.Contents.All(c => c is TextContent) ?
                    new ChatRequestUserMessage(string.Concat(input.Contents)) :
                    new ChatRequestUserMessage(GetContentParts(input.Contents));
            }
            else if (input.Role == ChatRole.Assistant)
            {
                // TODO: ChatRequestAssistantMessage only enables text content currently.
                // Update it with other content types when it supports that.
                ChatRequestAssistantMessage message = new(string.Concat(input.Contents.Where(c => c is TextContent)));

                foreach (var content in input.Contents)
                {
                    if (content is FunctionCallContent { CallId: not null } callRequest)
                    {
                        message.ToolCalls.Add(new ChatCompletionsToolCall(
                             callRequest.CallId,
                             new FunctionCall(
                                 callRequest.Name,
                                 JsonSerializer.Serialize(callRequest.Arguments, ToolCallJsonSerializerOptions.GetTypeInfo(typeof(IDictionary<string, object>))))));
                    }
                }

                yield return message;
            }
        }
    }

    /// <summary>Converts a list of <see cref="AIContent"/> to a list of <see cref="ChatMessageContentItem"/>.</summary>
    private static List<ChatMessageContentItem> GetContentParts(IList<AIContent> contents)
    {
        Debug.Assert(contents is { Count: > 0 }, "Expected non-empty contents");

        List<ChatMessageContentItem> parts = [];
        foreach (var content in contents)
        {
            switch (content)
            {
                case TextContent textContent:
                    parts.Add(new ChatMessageTextContentItem(textContent.Text));
                    break;

                case DataContent dataContent when dataContent.MediaTypeStartsWith("image/"):
                    if (dataContent.Data.HasValue)
                    {
                        parts.Add(new ChatMessageImageContentItem(BinaryData.FromBytes(dataContent.Data.Value), dataContent.MediaType));
                    }
                    else if (dataContent.Uri is string uri)
                    {
                        parts.Add(new ChatMessageImageContentItem(new Uri(uri)));
                    }

                    break;
            }
        }

        return parts;
    }

    private static FunctionCallContent ParseCallContentFromJsonString(string json, string callId, string name) =>
        FunctionCallContent.CreateFromParsedArguments(json, callId, name,
            argumentParser: static json => JsonSerializer.Deserialize(json,
                (JsonTypeInfo<IDictionary<string, object>>)AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(IDictionary<string, object>)))!);
}
