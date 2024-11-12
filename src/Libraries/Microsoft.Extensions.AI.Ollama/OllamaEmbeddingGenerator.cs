﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IEmbeddingGenerator{String, Embedding}"/> for Ollama.</summary>
public sealed class OllamaEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    /// <summary>The api/embeddings endpoint URI.</summary>
    private readonly Uri _apiEmbeddingsEndpoint;

    /// <summary>The <see cref="HttpClient"/> to use for sending requests.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>Initializes a new instance of the <see cref="OllamaEmbeddingGenerator"/> class.</summary>
    /// <param name="endpoint">The endpoint URI where Ollama is hosted.</param>
    /// <param name="modelId">
    /// The ID of the model to use. This ID can also be overridden per request via <see cref="ChatOptions.ModelId"/>.
    /// Either this parameter or <see cref="ChatOptions.ModelId"/> must provide a valid model ID.
    /// </param>
    /// <param name="httpClient">An <see cref="HttpClient"/> instance to use for HTTP operations.</param>
    public OllamaEmbeddingGenerator(string endpoint, string? modelId = null, HttpClient? httpClient = null)
        : this(new Uri(Throw.IfNull(endpoint)), modelId, httpClient)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="OllamaEmbeddingGenerator"/> class.</summary>
    /// <param name="endpoint">The endpoint URI where Ollama is hosted.</param>
    /// <param name="modelId">
    /// The ID of the model to use. This ID can also be overridden per request via <see cref="ChatOptions.ModelId"/>.
    /// Either this parameter or <see cref="ChatOptions.ModelId"/> must provide a valid model ID.
    /// </param>
    /// <param name="httpClient">An <see cref="HttpClient"/> instance to use for HTTP operations.</param>
    public OllamaEmbeddingGenerator(Uri endpoint, string? modelId = null, HttpClient? httpClient = null)
    {
        _ = Throw.IfNull(endpoint);
        if (modelId is not null)
        {
            _ = Throw.IfNullOrWhitespace(modelId);
        }

        _apiEmbeddingsEndpoint = new Uri(endpoint, "api/embed");
        _httpClient = httpClient ?? OllamaUtilities.SharedClient;
        Metadata = new("ollama", endpoint, modelId);
    }

    /// <inheritdoc />
    public EmbeddingGeneratorMetadata Metadata { get; }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_httpClient != OllamaUtilities.SharedClient)
        {
            _httpClient.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(values);

        // Create request.
        string[] inputs = values.ToArray();
        string? requestModel = options?.ModelId ?? Metadata.ModelId;
        var request = new OllamaEmbeddingRequest
        {
            Model = requestModel ?? string.Empty,
            Input = inputs,
        };

        if (options?.AdditionalProperties is { } requestProps)
        {
            if (requestProps.TryGetValue("keep_alive", out long keepAlive))
            {
                request.KeepAlive = keepAlive;
            }

            if (requestProps.TryGetValue("truncate", out bool truncate))
            {
                request.Truncate = truncate;
            }
        }

        // Send request and get response.
        var httpResponse = await _httpClient.PostAsJsonAsync(
            _apiEmbeddingsEndpoint,
            request,
            JsonContext.Default.OllamaEmbeddingRequest,
            cancellationToken).ConfigureAwait(false);

        var response = (await httpResponse.Content.ReadFromJsonAsync(
            JsonContext.Default.OllamaEmbeddingResponse,
            cancellationToken).ConfigureAwait(false))!;

        // Validate response.
        if (!string.IsNullOrEmpty(response.Error))
        {
            throw new InvalidOperationException($"Ollama error: {response.Error}");
        }

        if (response.Embeddings is null || response.Embeddings.Length != inputs.Length)
        {
            throw new InvalidOperationException($"Ollama generated {response.Embeddings?.Length ?? 0} embeddings but {inputs.Length} were expected.");
        }

        // Convert response into result objects.
        AdditionalPropertiesDictionary? responseProps = null;
        OllamaUtilities.TransferNanosecondsTime(response, r => r.TotalDuration, "total_duration", ref responseProps);
        OllamaUtilities.TransferNanosecondsTime(response, r => r.LoadDuration, "load_duration", ref responseProps);

        UsageDetails? usage = null;
        if (response.PromptEvalCount is int tokens)
        {
            usage = new()
            {
                InputTokenCount = tokens,
                TotalTokenCount = tokens,
            };
        }

        return new(response.Embeddings.Select(e =>
            new Embedding<float>(e)
            {
                CreatedAt = DateTimeOffset.UtcNow,
                ModelId = response.Model ?? requestModel,
            }))
        {
            Usage = usage,
            AdditionalProperties = responseProps,
        };
    }
}
