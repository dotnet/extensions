// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Embeddings;
using OpenAI.Responses;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="OpenAIClient"/>s.</summary>
public static class OpenAIClientExtensions
{
    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="OpenAIClient"/>.</summary>
    /// <param name="openAIClient">The client.</param>
    /// <param name="modelId">The model.</param>
    /// <returns>An <see cref="IChatClient"/> that can be used to converse via the <see cref="OpenAIClient"/>.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This method will be removed in an upcoming release.")]
    public static IChatClient AsChatClient(this OpenAIClient openAIClient, string modelId) =>
        new OpenAIChatClient(Throw.IfNull(openAIClient).GetChatClient(modelId));

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

    /// <summary>Gets an <see cref="ISpeechToTextClient"/> for use with this <see cref="AudioClient"/>.</summary>
    /// <param name="audioClient">The client.</param>
    /// <returns>An <see cref="ISpeechToTextClient"/> that can be used to transcribe audio via the <see cref="AudioClient"/>.</returns>
    [Experimental("MEAI001")]
    public static ISpeechToTextClient AsISpeechToTextClient(this AudioClient audioClient) =>
        new OpenAISpeechToTextClient(audioClient);

    /// <summary>Gets an <see cref="IEmbeddingGenerator{String, Single}"/> for use with this <see cref="OpenAIClient"/>.</summary>
    /// <param name="openAIClient">The client.</param>
    /// <param name="modelId">The model to use.</param>
    /// <param name="dimensions">The number of dimensions to generate in each embedding.</param>
    /// <returns>An <see cref="IEmbeddingGenerator{String, Embedding}"/> that can be used to generate embeddings via the <see cref="EmbeddingClient"/>.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This method will be removed in an upcoming release.")]
    public static IEmbeddingGenerator<string, Embedding<float>> AsEmbeddingGenerator(this OpenAIClient openAIClient, string modelId, int? dimensions = null) =>
        new OpenAIEmbeddingGenerator(Throw.IfNull(openAIClient).GetEmbeddingClient(modelId), dimensions);

    /// <summary>Gets an <see cref="IEmbeddingGenerator{String, Single}"/> for use with this <see cref="EmbeddingClient"/>.</summary>
    /// <param name="embeddingClient">The client.</param>
    /// <param name="defaultModelDimensions">The number of dimensions to generate in each embedding.</param>
    /// <returns>An <see cref="IEmbeddingGenerator{String, Embedding}"/> that can be used to generate embeddings via the <see cref="EmbeddingClient"/>.</returns>
    public static IEmbeddingGenerator<string, Embedding<float>> AsIEmbeddingGenerator(this EmbeddingClient embeddingClient, int? defaultModelDimensions = null) =>
        new OpenAIEmbeddingGenerator(embeddingClient, defaultModelDimensions);
}
