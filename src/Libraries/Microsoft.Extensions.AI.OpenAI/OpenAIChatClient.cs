// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI;
using OpenAI.Chat;

#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1108 // Block statements should not contain embedded comments

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IChatClient"/> for an OpenAI <see cref="OpenAIClient"/> or <see cref="ChatClient"/>.</summary>
public sealed class OpenAIChatClient : IChatClient
{
    /// <summary>Gets the default OpenAI endpoint.</summary>
    internal static Uri DefaultOpenAIEndpoint { get; } = new("https://api.openai.com/v1");

    /// <summary>Metadata about the client.</summary>
    private readonly ChatClientMetadata _metadata;

    /// <summary>The underlying <see cref="OpenAIClient" />.</summary>
    private readonly OpenAIClient? _openAIClient;

    /// <summary>The underlying <see cref="ChatClient" />.</summary>
    private readonly ChatClient _chatClient;

    /// <summary>The <see cref="JsonSerializerOptions"/> use for any serialization activities related to tool call arguments and results.</summary>
    private JsonSerializerOptions _toolCallJsonSerializerOptions = AIJsonUtilities.DefaultOptions;

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
            ?.GetValue(openAIClient) as Uri ?? DefaultOpenAIEndpoint;

        _metadata = new("openai", providerUrl, modelId);
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
            ?.GetValue(chatClient) as Uri ?? DefaultOpenAIEndpoint;
        string? model = typeof(ChatClient).GetField("_model", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(chatClient) as string;

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
            serviceType == typeof(OpenAIClient) ? _openAIClient :
            serviceType == typeof(ChatClient) ? _chatClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        var openAIChatMessages = OpenAIModelMappers.ToOpenAIChatMessages(chatMessages, ToolCallJsonSerializerOptions);
        var openAIOptions = OpenAIModelMappers.ToOpenAIOptions(options);

        // Make the call to OpenAI.
        var response = await _chatClient.CompleteChatAsync(openAIChatMessages, openAIOptions, cancellationToken).ConfigureAwait(false);

        return OpenAIModelMappers.FromOpenAIChatCompletion(response.Value, options, openAIOptions);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chatMessages);

        var openAIChatMessages = OpenAIModelMappers.ToOpenAIChatMessages(chatMessages, ToolCallJsonSerializerOptions);
        var openAIOptions = OpenAIModelMappers.ToOpenAIOptions(options);

        // Make the call to OpenAI.
        var chatCompletionUpdates = _chatClient.CompleteChatStreamingAsync(openAIChatMessages, openAIOptions, cancellationToken);

        return OpenAIModelMappers.FromOpenAIStreamingChatCompletionAsync(chatCompletionUpdates, cancellationToken);
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IChatClient interface.
    }
}
