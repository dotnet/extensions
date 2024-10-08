// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

#pragma warning disable EA0000 // Use source generated logging methods for improved performance

namespace Microsoft.Extensions.AI;

/// <summary>A delegating embedding generator that logs embedding generation operations to an <see cref="ILogger"/>.</summary>
/// <typeparam name="TInput">Specifies the type of the input passed to the generator.</typeparam>
/// <typeparam name="TEmbedding">Specifies the type of the embedding instance produced by the generator.</typeparam>
public class LoggingEmbeddingGenerator<TInput, TEmbedding> : DelegatingEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    /// <summary>An <see cref="ILogger"/> instance used for all logging.</summary>
    private readonly ILogger _logger;

    /// <summary>The <see cref="JsonSerializerOptions"/> to use for serialization of state written to the logger.</summary>
    private JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>Initializes a new instance of the <see cref="LoggingEmbeddingGenerator{TInput, TEmbedding}"/> class.</summary>
    /// <param name="innerGenerator">The underlying <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance that will be used for all logging.</param>
    public LoggingEmbeddingGenerator(IEmbeddingGenerator<TInput, TEmbedding> innerGenerator, ILogger logger)
        : base(innerGenerator)
    {
        _logger = Throw.IfNull(logger);
        _jsonSerializerOptions = JsonDefaults.Options;
    }

    /// <summary>Gets or sets JSON serialization options to use when serializing logging data.</summary>
    public JsonSerializerOptions JsonSerializerOptions
    {
        get => _jsonSerializerOptions;
        set => _jsonSerializerOptions = Throw.IfNull(value);
    }

    /// <inheritdoc/>
    public override async Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(IEnumerable<TInput> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.Log(LogLevel.Trace, 0, (values, options, this), null, static (state, _) =>
                    "GenerateAsync invoked: " +
                    $"Values: {JsonSerializer.Serialize(state.values, state.Item3._jsonSerializerOptions.GetTypeInfo(typeof(IEnumerable<TInput>)))}. " +
                    $"Options: {JsonSerializer.Serialize(state.options, state.Item3._jsonSerializerOptions.GetTypeInfo(typeof(EmbeddingGenerationOptions)))}. " +
                    $"Metadata: {JsonSerializer.Serialize(state.Item3.Metadata, state.Item3._jsonSerializerOptions.GetTypeInfo(typeof(EmbeddingGeneratorMetadata)))}.");
            }
            else
            {
                _logger.LogDebug("GenerateAsync invoked.");
            }
        }

        try
        {
            var embeddings = await base.GenerateAsync(values, options, cancellationToken).ConfigureAwait(false);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("GenerateAsync generated {Count} embedding(s).", embeddings.Count);
            }

            return embeddings;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "GenerateAsync failed.");
            throw;
        }
    }
}
