// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI;
using OpenAI.Embeddings;

#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

namespace Microsoft.Extensions.AI;

/// <summary>An <see cref="IEmbeddingGenerator{String, Embedding}"/> for an OpenAI <see cref="EmbeddingClient"/>.</summary>
public sealed class OpenAIEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    /// <summary>Default OpenAI endpoint.</summary>
    private const string DefaultOpenAIEndpoint = "https://api.openai.com/v1";

    /// <summary>Metadata about the embedding generator.</summary>
    private readonly EmbeddingGeneratorMetadata _metadata;

    /// <summary>The underlying <see cref="OpenAIClient" />.</summary>
    private readonly OpenAIClient? _openAIClient;

    /// <summary>The underlying <see cref="OpenAI.Chat.ChatClient" />.</summary>
    private readonly EmbeddingClient _embeddingClient;

    /// <summary>The number of dimensions produced by the generator.</summary>
    private readonly int? _dimensions;

    /// <summary>Initializes a new instance of the <see cref="OpenAIEmbeddingGenerator"/> class.</summary>
    /// <param name="openAIClient">The underlying client.</param>
    /// <param name="modelId">The model to use.</param>
    /// <param name="dimensions">The number of dimensions to generate in each embedding.</param>
    public OpenAIEmbeddingGenerator(
        OpenAIClient openAIClient, string modelId, int? dimensions = null)
    {
        _ = Throw.IfNull(openAIClient);
        _ = Throw.IfNullOrWhitespace(modelId);
        if (dimensions is < 1)
        {
            Throw.ArgumentOutOfRangeException(nameof(dimensions), "Value must be greater than 0.");
        }

        _openAIClient = openAIClient;
        _embeddingClient = openAIClient.GetEmbeddingClient(modelId);
        _dimensions = dimensions;

        // https://github.com/openai/openai-dotnet/issues/215
        // The endpoint isn't currently exposed, so use reflection to get at it, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        string providerUrl = (typeof(OpenAIClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(openAIClient) as Uri)?.ToString() ??
            DefaultOpenAIEndpoint;

        _metadata = CreateMetadata("openai", providerUrl, modelId, dimensions);
    }

    /// <summary>Initializes a new instance of the <see cref="OpenAIEmbeddingGenerator"/> class.</summary>
    /// <param name="embeddingClient">The underlying client.</param>
    /// <param name="dimensions">The number of dimensions to generate in each embedding.</param>
    public OpenAIEmbeddingGenerator(EmbeddingClient embeddingClient, int? dimensions = null)
    {
        _ = Throw.IfNull(embeddingClient);
        if (dimensions < 1)
        {
            Throw.ArgumentOutOfRangeException(nameof(dimensions), "Value must be greater than 0.");
        }

        _embeddingClient = embeddingClient;
        _dimensions = dimensions;

        // https://github.com/openai/openai-dotnet/issues/215
        // The endpoint and model aren't currently exposed, so use reflection to get at them, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        string providerUrl = (typeof(EmbeddingClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(embeddingClient) as Uri)?.ToString() ??
            DefaultOpenAIEndpoint;

        FieldInfo? modelField = typeof(EmbeddingClient).GetField("_model", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        string? model = modelField?.GetValue(embeddingClient) as string;

        _metadata = CreateMetadata("openai", providerUrl, model, dimensions);
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
    object? IEmbeddingGenerator<string, Embedding<float>>.GetService(Type serviceType, object? serviceKey)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(EmbeddingGeneratorMetadata) ? _metadata :
            serviceType == typeof(OpenAIClient) ? _openAIClient :
            serviceType == typeof(EmbeddingClient) ? _embeddingClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <summary>Creates the <see cref="EmbeddingGeneratorMetadata"/> for this instance.</summary>
    private static EmbeddingGeneratorMetadata CreateMetadata(string providerName, string providerUrl, string? model, int? dimensions) =>
        new(providerName, Uri.TryCreate(providerUrl, UriKind.Absolute, out Uri? providerUri) ? providerUri : null, model, dimensions);

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    private OpenAI.Embeddings.EmbeddingGenerationOptions? ToOpenAIOptions(EmbeddingGenerationOptions? options)
    {
        OpenAI.Embeddings.EmbeddingGenerationOptions openAIOptions = new()
        {
            Dimensions = options?.Dimensions ?? _dimensions,
        };

        if (options?.AdditionalProperties is { Count: > 0 } additionalProperties)
        {
            if (additionalProperties.TryGetValue(nameof(openAIOptions.EndUserId), out string? endUserId))
            {
                openAIOptions.EndUserId = endUserId;
            }
        }

        return openAIOptions;
    }
}
