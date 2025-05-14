// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.Inference;
using Microsoft.Shared.Diagnostics;

#pragma warning disable EA0002 // Use 'System.TimeProvider' to make the code easier to test
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable S109 // Magic numbers should not be used

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IEmbeddingGenerator{DataContent, Embedding}"/> for an Azure.AI.Inference <see cref="ImageEmbeddingsClient"/>.</summary>
internal sealed class AzureAIInferenceImageEmbeddingGenerator :
    IEmbeddingGenerator<DataContent, Embedding<float>>
{
    /// <summary>Metadata about the embedding generator.</summary>
    private readonly EmbeddingGeneratorMetadata _metadata;

    /// <summary>The underlying <see cref="ImageEmbeddingsClient" />.</summary>
    private readonly ImageEmbeddingsClient _imageEmbeddingsClient;

    /// <summary>The number of dimensions produced by the generator.</summary>
    private readonly int? _dimensions;

    /// <summary>Initializes a new instance of the <see cref="AzureAIInferenceImageEmbeddingGenerator"/> class.</summary>
    /// <param name="imageEmbeddingsClient">The underlying client.</param>
    /// <param name="defaultModelId">
    /// The ID of the model to use. This can also be overridden per request via <see cref="EmbeddingGenerationOptions.ModelId"/>.
    /// Either this parameter or <see cref="EmbeddingGenerationOptions.ModelId"/> must provide a valid model ID.
    /// </param>
    /// <param name="defaultModelDimensions">The number of dimensions to generate in each embedding.</param>
    /// <exception cref="ArgumentNullException"><paramref name="imageEmbeddingsClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="defaultModelId"/> is empty or composed entirely of whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="defaultModelDimensions"/> is not positive.</exception>
    public AzureAIInferenceImageEmbeddingGenerator(
        ImageEmbeddingsClient imageEmbeddingsClient, string? defaultModelId = null, int? defaultModelDimensions = null)
    {
        _ = Throw.IfNull(imageEmbeddingsClient);

        if (defaultModelId is not null)
        {
            _ = Throw.IfNullOrWhitespace(defaultModelId);
        }

        if (defaultModelDimensions is < 1)
        {
            Throw.ArgumentOutOfRangeException(nameof(defaultModelDimensions), "Value must be greater than 0.");
        }

        _imageEmbeddingsClient = imageEmbeddingsClient;
        _dimensions = defaultModelDimensions;

        // https://github.com/Azure/azure-sdk-for-net/issues/46278
        // The endpoint isn't currently exposed, so use reflection to get at it, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        var providerUrl = typeof(ImageEmbeddingsClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(imageEmbeddingsClient) as Uri;

        _metadata = new EmbeddingGeneratorMetadata("az.ai.inference", providerUrl, defaultModelId, defaultModelDimensions);
    }

    /// <inheritdoc />
    object? IEmbeddingGenerator.GetService(Type serviceType, object? serviceKey)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(ImageEmbeddingsClient) ? _imageEmbeddingsClient :
            serviceType == typeof(EmbeddingGeneratorMetadata) ? _metadata :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<DataContent> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(values);

        var azureAIOptions = ToAzureAIOptions(values, options);

        var embeddings = (await _imageEmbeddingsClient.EmbedAsync(azureAIOptions, cancellationToken).ConfigureAwait(false)).Value;

        GeneratedEmbeddings<Embedding<float>> result = new(embeddings.Data.Select(e =>
            new Embedding<float>(AzureAIInferenceEmbeddingGenerator.ParseBase64Floats(e.Embedding))
            {
                CreatedAt = DateTimeOffset.UtcNow,
                ModelId = embeddings.Model ?? azureAIOptions.Model,
            }));

        if (embeddings.Usage is not null)
        {
            result.Usage = new()
            {
                InputTokenCount = embeddings.Usage.PromptTokens,
                TotalTokenCount = embeddings.Usage.TotalTokens
            };
        }

        return result;
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IEmbeddingGenerator interface.
    }

    /// <summary>Converts an extensions options instance to an Azure.AI.Inference options instance.</summary>
    /// <remarks>
    /// Note: we can't currently use RawRepresentationFactory due to https://github.com/Azure/azure-sdk-for-net/issues/50018.
    /// </remarks>
    private ImageEmbeddingsOptions ToAzureAIOptions(IEnumerable<DataContent> inputs, EmbeddingGenerationOptions? options)
    {
        ImageEmbeddingsOptions result = new(inputs.Select(dc => new ImageEmbeddingInput(dc.Uri)))
        {
            Dimensions = options?.Dimensions ?? _dimensions,
            Model = options?.ModelId ?? _metadata.DefaultModelId,
            EncodingFormat = EmbeddingEncodingFormat.Base64,
        };

        if (options?.AdditionalProperties is { } props)
        {
            foreach (var prop in props)
            {
                if (prop.Value is not null)
                {
                    byte[] data = JsonSerializer.SerializeToUtf8Bytes(prop.Value, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(object)));
                    result.AdditionalProperties[prop.Key] = new BinaryData(data);
                }
            }
        }

        return result;
    }
}
