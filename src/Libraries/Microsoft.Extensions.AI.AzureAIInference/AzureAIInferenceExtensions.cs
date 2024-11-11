// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.AI.Inference;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with Azure AI Inference.</summary>
public static class AzureAIInferenceExtensions
{
    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="ChatCompletionsClient"/>.</summary>
    /// <param name="chatCompletionsClient">The client.</param>
    /// <param name="modelId">The ID of the model to use. If null, it can be provided per request via <see cref="ChatOptions.ModelId"/>.</param>
    /// <returns>An <see cref="IChatClient"/> that can be used to converse via the <see cref="ChatCompletionsClient"/>.</returns>
    public static IChatClient AsChatClient(
        this ChatCompletionsClient chatCompletionsClient, string? modelId = null) =>
        new AzureAIInferenceChatClient(chatCompletionsClient, modelId);

    /// <summary>Gets an <see cref="IEmbeddingGenerator{String, Single}"/> for use with this <see cref="EmbeddingsClient"/>.</summary>
    /// <param name="embeddingsClient">The client.</param>
    /// <param name="modelId">The ID of the model to use. If null, it can be provided per request via <see cref="ChatOptions.ModelId"/>.</param>
    /// <param name="dimensions">The number of dimensions to generate in each embedding.</param>
    /// <returns>An <see cref="IEmbeddingGenerator{String, Embedding}"/> that can be used to generate embeddings via the <see cref="EmbeddingsClient"/>.</returns>
    public static IEmbeddingGenerator<string, Embedding<float>> AsEmbeddingGenerator(
        this EmbeddingsClient embeddingsClient, string? modelId = null, int? dimensions = null) =>
        new AzureAIInferenceEmbeddingGenerator(embeddingsClient, modelId, dimensions);
}
