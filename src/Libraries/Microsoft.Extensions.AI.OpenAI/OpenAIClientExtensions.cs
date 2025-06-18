// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Embeddings;
using OpenAI.Responses;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="OpenAIClient"/>s.</summary>
public static class OpenAIClientExtensions
{
    /// <summary>Key into AdditionalProperties used to store a strict option.</summary>
    internal const string StrictKey = "strictJsonSchema";

    /// <summary>Gets the default OpenAI endpoint.</summary>
    internal static Uri DefaultOpenAIEndpoint { get; } = new("https://api.openai.com/v1");

    /// <summary>Gets a <see cref="ChatRole"/> for "developer".</summary>
    internal static ChatRole ChatRoleDeveloper { get; } = new ChatRole("developer");

    /// <summary>
    /// Gets the JSON schema transformer cache conforming to OpenAI <b>strict</b> restrictions per https://platform.openai.com/docs/guides/structured-outputs?api-mode=responses#supported-schemas.
    /// </summary>
    internal static AIJsonSchemaTransformCache StrictSchemaTransformCache { get; } = new(new()
    {
        DisallowAdditionalProperties = true,
        ConvertBooleanSchemas = true,
        MoveDefaultKeywordToDescription = true,
        RequireAllProperties = true,
    });

    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="ChatClient"/>.</summary>
    /// <param name="chatClient">The client.</param>
    /// <returns>An <see cref="IChatClient"/> that can be used to converse via the <see cref="ChatClient"/>.</returns>
    public static IChatClient AsIChatClient(this ChatClient chatClient) =>
        new OpenAIChatClient(chatClient);

    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="OpenAIResponseClient"/>.</summary>
    /// <param name="responseClient">The client.</param>
    /// <returns>An <see cref="IChatClient"/> that can be used to converse via the <see cref="OpenAIResponseClient"/>.</returns>
    public static IChatClient AsIChatClient(this OpenAIResponseClient responseClient) =>
        new OpenAIResponseChatClient(responseClient);

    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="AssistantClient"/>.</summary>
    /// <param name="assistantClient">The <see cref="AssistantClient"/> instance to be accessed as an <see cref="IChatClient"/>.</param>
    /// <param name="assistantId">The unique identifier of the assistant with which to interact.</param>
    /// <param name="threadId">
    /// An optional existing thread identifier for the chat session. This serves as a default, and may be overridden per call to
    /// <see cref="IChatClient.GetResponseAsync"/> or <see cref="IChatClient.GetStreamingResponseAsync"/> via the <see cref="ChatOptions.ConversationId"/>
    /// property. If no thread ID is provided via either mechanism, a new thread will be created for the request.
    /// </param>
    /// <returns>An <see cref="IChatClient"/> instance configured to interact with the specified agent and thread.</returns>
    [Experimental("OPENAI001")]
    public static IChatClient AsIChatClient(this AssistantClient assistantClient, string assistantId, string? threadId = null) =>
        new OpenAIAssistantChatClient(assistantClient, assistantId, threadId);

    /// <summary>Gets an <see cref="ISpeechToTextClient"/> for use with this <see cref="AudioClient"/>.</summary>
    /// <param name="audioClient">The client.</param>
    /// <returns>An <see cref="ISpeechToTextClient"/> that can be used to transcribe audio via the <see cref="AudioClient"/>.</returns>
    [Experimental("MEAI001")]
    public static ISpeechToTextClient AsISpeechToTextClient(this AudioClient audioClient) =>
        new OpenAISpeechToTextClient(audioClient);

    /// <summary>Gets an <see cref="IEmbeddingGenerator{String, Single}"/> for use with this <see cref="EmbeddingClient"/>.</summary>
    /// <param name="embeddingClient">The client.</param>
    /// <param name="defaultModelDimensions">The number of dimensions to generate in each embedding.</param>
    /// <returns>An <see cref="IEmbeddingGenerator{String, Embedding}"/> that can be used to generate embeddings via the <see cref="EmbeddingClient"/>.</returns>
    public static IEmbeddingGenerator<string, Embedding<float>> AsIEmbeddingGenerator(this EmbeddingClient embeddingClient, int? defaultModelDimensions = null) =>
        new OpenAIEmbeddingGenerator(embeddingClient, defaultModelDimensions);

    /// <summary>Gets the JSON schema to use from the function.</summary>
    internal static JsonElement GetSchema(AIFunction function, bool? strict) =>
        strict is true ?
            StrictSchemaTransformCache.GetOrCreateTransformedSchema(function) :
            function.JsonSchema;
}
