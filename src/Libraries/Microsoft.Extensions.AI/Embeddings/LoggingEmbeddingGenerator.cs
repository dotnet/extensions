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

namespace Microsoft.Extensions.AI;

/// <summary>A delegating embedding generator that logs embedding generation operations to an <see cref="ILogger"/>.</summary>
/// <typeparam name="TInput">Specifies the type of the input passed to the generator.</typeparam>
/// <typeparam name="TEmbedding">Specifies the type of the embedding instance produced by the generator.</typeparam>
/// <para>
/// The provided implementation of <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> is thread-safe for concurrent use
/// so long as the <see cref="ILogger"/> employed is also thread-safe for concurrent use.
/// </para>
public partial class LoggingEmbeddingGenerator<TInput, TEmbedding> : DelegatingEmbeddingGenerator<TInput, TEmbedding>
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
        _jsonSerializerOptions = AIJsonUtilities.DefaultOptions;
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
                LogInvokedSensitive(AsJson(values), AsJson(options), AsJson(this.GetService<TInput, TEmbedding, EmbeddingGeneratorMetadata>()));
            }
            else
            {
                LogInvoked();
            }
        }

        try
        {
            var embeddings = await base.GenerateAsync(values, options, cancellationToken).ConfigureAwait(false);

            LogCompleted(embeddings.Count);

            return embeddings;
        }
        catch (OperationCanceledException)
        {
            LogInvocationCanceled();
            throw;
        }
        catch (Exception ex)
        {
            LogInvocationFailed(ex);
            throw;
        }
    }

    private string AsJson<T>(T value) => JsonSerializer.Serialize(value, _jsonSerializerOptions.GetTypeInfo(typeof(T)));

    [LoggerMessage(LogLevel.Debug, "GenerateAsync invoked.")]
    private partial void LogInvoked();

    [LoggerMessage(LogLevel.Trace, "GenerateAsync invoked: {Values}. Options: {EmbeddingGenerationOptions}. Metadata: {EmbeddingGeneratorMetadata}.")]
    private partial void LogInvokedSensitive(string values, string embeddingGenerationOptions, string embeddingGeneratorMetadata);

    [LoggerMessage(LogLevel.Debug, "GenerateAsync generated {EmbeddingsCount} embedding(s).")]
    private partial void LogCompleted(int embeddingsCount);

    [LoggerMessage(LogLevel.Debug, "GenerateAsync canceled.")]
    private partial void LogInvocationCanceled();

    [LoggerMessage(LogLevel.Error, "GenerateAsync failed.")]
    private partial void LogInvocationFailed(Exception error);
}
