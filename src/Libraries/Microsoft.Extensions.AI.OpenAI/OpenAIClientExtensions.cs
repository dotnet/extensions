// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OpenAI;
using OpenAI.Assistants;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="OpenAIClient"/>s.</summary>
public static class OpenAIClientExtensions
{
    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="OpenAIClient"/>.</summary>
    /// <param name="openAIClient">The client.</param>
    /// <param name="modelId">The model.</param>
    /// <returns>An <see cref="IChatClient"/> that can be used to converse via the <see cref="OpenAIClient"/>.</returns>
    public static IChatClient AsChatClient(this OpenAIClient openAIClient, string modelId) =>
        new OpenAIChatClient(openAIClient, modelId);

    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="ChatClient"/>.</summary>
    /// <param name="chatClient">The client.</param>
    /// <returns>An <see cref="IChatClient"/> that can be used to converse via the <see cref="ChatClient"/>.</returns>
    public static IChatClient AsChatClient(this ChatClient chatClient) =>
        new OpenAIChatClient(chatClient);

#pragma warning disable OPENAI001 // Type is for evaluation purposes only
    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="AssistantClient"/>.</summary>
    /// <param name="assistantClient">The client.</param>
    /// <param name="assistantId">The ID of the assistant to use.</param>
    /// <param name="threadId">
    /// The ID of the thread to use. If not supplied here, it should be supplied per request in <see cref="ChatOptions.ChatThreadId"/>.
    /// If none is supplied, a new thread will be created for a request.
    /// </param>
    /// <returns>An <see cref="IChatClient"/> that can be used to converse via the <see cref="ChatClient"/>.</returns>
    public static IChatClient AsChatClient(this AssistantClient assistantClient, string assistantId, string? threadId = null) =>
        new OpenAIAssistantClient(assistantClient, assistantId, threadId);
#pragma warning restore OPENAI001

    /// <summary>Gets an <see cref="IEmbeddingGenerator{String, Single}"/> for use with this <see cref="OpenAIClient"/>.</summary>
    /// <param name="openAIClient">The client.</param>
    /// <param name="modelId">The model to use.</param>
    /// <param name="dimensions">The number of dimensions to generate in each embedding.</param>
    /// <returns>An <see cref="IEmbeddingGenerator{String, Embedding}"/> that can be used to generate embeddings via the <see cref="EmbeddingClient"/>.</returns>
    public static IEmbeddingGenerator<string, Embedding<float>> AsEmbeddingGenerator(this OpenAIClient openAIClient, string modelId, int? dimensions = null) =>
        new OpenAIEmbeddingGenerator(openAIClient, modelId, dimensions);

    /// <summary>Gets an <see cref="IEmbeddingGenerator{String, Single}"/> for use with this <see cref="EmbeddingClient"/>.</summary>
    /// <param name="embeddingClient">The client.</param>
    /// <param name="dimensions">The number of dimensions to generate in each embedding.</param>
    /// <returns>An <see cref="IEmbeddingGenerator{String, Embedding}"/> that can be used to generate embeddings via the <see cref="EmbeddingClient"/>.</returns>
    public static IEmbeddingGenerator<string, Embedding<float>> AsEmbeddingGenerator(this EmbeddingClient embeddingClient, int? dimensions = null) =>
        new OpenAIEmbeddingGenerator(embeddingClient, dimensions);
}
