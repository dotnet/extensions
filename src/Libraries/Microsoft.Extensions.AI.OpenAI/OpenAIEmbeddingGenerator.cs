// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI.Embeddings;

#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

namespace Microsoft.Extensions.AI;

/// <summary>An <see cref="IEmbeddingGenerator{String, Embedding}"/> for an OpenAI <see cref="EmbeddingClient"/>.</summary>
internal sealed class OpenAIEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    /// <summary>Metadata about the embedding generator.</summary>
    private readonly EmbeddingGeneratorMetadata _metadata;

    /// <summary>The underlying <see cref="OpenAI.Chat.ChatClient" />.</summary>
    private readonly EmbeddingClient _embeddingClient;

    /// <summary>The number of dimensions produced by the generator.</summary>
    private readonly int? _dimensions;

    /// <summary>Initializes a new instance of the <see cref="OpenAIEmbeddingGenerator"/> class.</summary>
    /// <param name="embeddingClient">The underlying client.</param>
    /// <param name="defaultModelDimensions">The number of dimensions to generate in each embedding.</param>
    /// <exception cref="ArgumentNullException"><paramref name="embeddingClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="defaultModelDimensions"/> is not positive.</exception>
    public OpenAIEmbeddingGenerator(EmbeddingClient embeddingClient, int? defaultModelDimensions = null)
    {
        _embeddingClient = Throw.IfNull(embeddingClient);
        _dimensions = defaultModelDimensions;

        if (defaultModelDimensions < 1)
        {
            Throw.ArgumentOutOfRangeException(nameof(defaultModelDimensions), "Value must be greater than 0.");
        }

        _metadata = new("openai", embeddingClient.Endpoint, _embeddingClient.Model, defaultModelDimensions);
    }

    /// <inheritdoc />
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        OpenAI.Embeddings.EmbeddingGenerationOptions? openAIOptions = ToOpenAIOptions(options);

        var embeddings = (await _embeddingClient.GenerateEmbeddingsAsync(values, openAIOptions, cancellationToken).ConfigureAwait(false)).Value;

        return new(embeddings.Select(e =>
                new Embedding<float>(e.ToFloats())
                {
                    CreatedAt = DateTimeOffset.UtcNow,
                    ModelId = embeddings.Model,
                }))
        {
            Usage = new()
            {
                InputTokenCount = embeddings.Usage.InputTokenCount,
                TotalTokenCount = embeddings.Usage.TotalTokenCount
            },
        };
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IEmbeddingGenerator interface.
    }

    /// <inheritdoc />
    object? IEmbeddingGenerator.GetService(Type serviceType, object? serviceKey)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(EmbeddingGeneratorMetadata) ? _metadata :
            serviceType == typeof(EmbeddingClient) ? _embeddingClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    private OpenAI.Embeddings.EmbeddingGenerationOptions ToOpenAIOptions(EmbeddingGenerationOptions? options)
    {
        if (options?.RawRepresentationFactory?.Invoke(this) is not OpenAI.Embeddings.EmbeddingGenerationOptions result)
        {
            result = new OpenAI.Embeddings.EmbeddingGenerationOptions();
        }

        result.Dimensions ??= options?.Dimensions ?? _dimensions;
        return result;
    }
}
