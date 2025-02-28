// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.Inference;
using Microsoft.Shared.Diagnostics;

#pragma warning disable EA0002 // Use 'System.TimeProvider' to make the code easier to test
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable S109 // Magic numbers should not be used

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IEmbeddingGenerator{String, Embedding}"/> for an Azure.AI.Inference <see cref="EmbeddingsClient"/>.</summary>
public sealed class AzureAIInferenceEmbeddingGenerator :
    IEmbeddingGenerator<string, Embedding<float>>
{
    /// <summary>Metadata about the embedding generator.</summary>
    private readonly EmbeddingGeneratorMetadata _metadata;

    /// <summary>The underlying <see cref="EmbeddingsClient" />.</summary>
    private readonly EmbeddingsClient _embeddingsClient;

    /// <summary>The number of dimensions produced by the generator.</summary>
    private readonly int? _dimensions;

    /// <summary>Initializes a new instance of the <see cref="AzureAIInferenceEmbeddingGenerator"/> class.</summary>
    /// <param name="embeddingsClient">The underlying client.</param>
    /// <param name="modelId">
    /// The ID of the model to use. This can also be overridden per request via <see cref="EmbeddingGenerationOptions.ModelId"/>.
    /// Either this parameter or <see cref="EmbeddingGenerationOptions.ModelId"/> must provide a valid model ID.
    /// </param>
    /// <param name="dimensions">The number of dimensions to generate in each embedding.</param>
    public AzureAIInferenceEmbeddingGenerator(
        EmbeddingsClient embeddingsClient, string? modelId = null, int? dimensions = null)
    {
        _ = Throw.IfNull(embeddingsClient);

        if (modelId is not null)
        {
            _ = Throw.IfNullOrWhitespace(modelId);
        }

        if (dimensions is < 1)
        {
            Throw.ArgumentOutOfRangeException(nameof(dimensions), "Value must be greater than 0.");
        }

        _embeddingsClient = embeddingsClient;
        _dimensions = dimensions;

        // https://github.com/Azure/azure-sdk-for-net/issues/46278
        // The endpoint isn't currently exposed, so use reflection to get at it, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        var providerUrl = typeof(EmbeddingsClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(embeddingsClient) as Uri;

        _metadata = new("az.ai.inference", providerUrl, modelId, dimensions);
    }

    /// <inheritdoc />
    object? IEmbeddingGenerator<string, Embedding<float>>.GetService(Type serviceType, object? serviceKey)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(EmbeddingsClient) ? _embeddingsClient :
            serviceType == typeof(EmbeddingGeneratorMetadata) ? _metadata :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var azureAIOptions = ToAzureAIOptions(values, options, EmbeddingEncodingFormat.Base64);

        var embeddings = (await _embeddingsClient.EmbedAsync(azureAIOptions, cancellationToken).ConfigureAwait(false)).Value;

        GeneratedEmbeddings<Embedding<float>> result = new(embeddings.Data.Select(e =>
            new Embedding<float>(ParseBase64Floats(e.Embedding))
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

    private static float[] ParseBase64Floats(BinaryData binaryData)
    {
        ReadOnlySpan<byte> base64 = binaryData.ToMemory().Span;

        // Remove quotes around base64 string.
        if (base64.Length < 2 || base64[0] != (byte)'"' || base64[base64.Length - 1] != (byte)'"')
        {
            ThrowInvalidData();
        }

        base64 = base64.Slice(1, base64.Length - 2);

        // Decode base64 string to bytes.
        byte[] bytes = ArrayPool<byte>.Shared.Rent(Base64.GetMaxDecodedFromUtf8Length(base64.Length));
        OperationStatus status = Base64.DecodeFromUtf8(base64, bytes.AsSpan(), out int bytesConsumed, out int bytesWritten);
        if (status != OperationStatus.Done || bytesWritten % sizeof(float) != 0)
        {
            ThrowInvalidData();
        }

        // Interpret bytes as floats
        float[] vector = new float[bytesWritten / sizeof(float)];
        bytes.AsSpan(0, bytesWritten).CopyTo(MemoryMarshal.AsBytes(vector.AsSpan()));
        if (!BitConverter.IsLittleEndian)
        {
            Span<int> ints = MemoryMarshal.Cast<float, int>(vector.AsSpan());
#if NET
            BinaryPrimitives.ReverseEndianness(ints, ints);
#else
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = BinaryPrimitives.ReverseEndianness(ints[i]);
            }
#endif
        }

        ArrayPool<byte>.Shared.Return(bytes);
        return vector;

        static void ThrowInvalidData() =>
            throw new FormatException("The input is not a valid Base64 string of encoded floats.");
    }

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    private EmbeddingsOptions ToAzureAIOptions(IEnumerable<string> inputs, EmbeddingGenerationOptions? options, EmbeddingEncodingFormat format)
    {
        EmbeddingsOptions result = new(inputs)
        {
            Dimensions = options?.Dimensions ?? _dimensions,
            Model = options?.ModelId ?? _metadata.ModelId,
            EncodingFormat = format,
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
