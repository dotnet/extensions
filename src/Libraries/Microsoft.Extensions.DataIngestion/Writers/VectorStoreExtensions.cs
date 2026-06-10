// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Provides extension methods for working with vector stores in the context of data ingestion.
/// </summary>
public static class VectorStoreExtensions
{
    /// <summary>
    /// Wraps an <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that accepts <see cref="string"/> inputs
    /// into one that accepts <see cref="AIContent"/> inputs, extracting text from <see cref="TextContent"/> instances.
    /// </summary>
    /// <typeparam name="TEmbedding">The type of the embedding produced by the generator.</typeparam>
    /// <param name="stringGenerator">The string-based embedding generator to wrap.</param>
    /// <returns>An <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that accepts <see cref="AIContent"/> inputs.</returns>
    public static IEmbeddingGenerator<AIContent, TEmbedding> AsAIContentEmbeddingGenerator<TEmbedding>(
        this IEmbeddingGenerator<string, TEmbedding> stringGenerator)
        where TEmbedding : Embedding
    {
        _ = Shared.Diagnostics.Throw.IfNull(stringGenerator);

        return new AIContentEmbeddingGeneratorAdapter<TEmbedding>(stringGenerator);
    }

    /// <summary>
    /// Wraps an <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that accepts <see cref="AIContent"/> inputs
    /// into one that accepts <see cref="string"/> inputs, wrapping each string as a <see cref="TextContent"/> instance.
    /// </summary>
    /// <typeparam name="TEmbedding">The type of the embedding produced by the generator.</typeparam>
    /// <param name="aiContentGenerator">The AIContent-based embedding generator to wrap.</param>
    /// <returns>An <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> that accepts <see cref="string"/> inputs.</returns>
    public static IEmbeddingGenerator<string, TEmbedding> AsStringEmbeddingGenerator<TEmbedding>(
        this IEmbeddingGenerator<AIContent, TEmbedding> aiContentGenerator)
        where TEmbedding : Embedding
    {
        _ = Shared.Diagnostics.Throw.IfNull(aiContentGenerator);

        return new StringEmbeddingGeneratorAdapter<TEmbedding>(aiContentGenerator);
    }

    /// <summary>
    /// Provides a convenient method to get a vector store collection specifically designed for storing ingested chunk records
    /// using the default <see cref="IngestionChunkVectorRecord"/> type.
    /// </summary>
    /// <param name="vectorStore">The vector store instance to create the collection in.</param>
    /// <param name="collectionName">The name of the collection to be created.</param>
    /// <param name="dimensionCount">The number of dimensions that the vector has.</param>
    /// <param name="distanceFunction">
    /// The distance function to use. When not provided, the default specific to given database will be used.
    /// Check <see cref="DistanceFunction"/> for available values.
    /// </param>
    /// <param name="indexKind">The index kind to use.</param>
    /// <returns>A vector store collection configured for ingested chunk records.</returns>
    [RequiresDynamicCode("This API is not compatible with NativeAOT. You can implement your own IngestionChunkWriter that uses dynamic mapping via VectorStore.GetCollectionDynamic().")]
    [RequiresUnreferencedCode("This API is not compatible with trimming. You can implement your own IngestionChunkWriter that uses dynamic mapping via VectorStore.GetCollectionDynamic().")]
    public static VectorStoreCollection<Guid, IngestionChunkVectorRecord> GetIngestionRecordCollection(this VectorStore vectorStore,
        string collectionName, int dimensionCount, string? distanceFunction = null, string? indexKind = null)
    {
        return vectorStore.GetIngestionRecordCollection<IngestionChunkVectorRecord>(collectionName, dimensionCount, distanceFunction, indexKind);
    }

    /// <summary>
    /// Provides a convenient method to get a vector store collection specifically designed for storing ingested chunk records.
    /// </summary>
    /// <typeparam name="TRecord">The type of the record to be stored in the collection.</typeparam>
    /// <param name="vectorStore">The vector store instance to create the collection in.</param>
    /// <param name="collectionName">The name of the collection to be created.</param>
    /// <param name="dimensionCount">The number of dimensions that the vector has.</param>
    /// <param name="distanceFunction">
    /// The distance function to use. When not provided, the default specific to given database will be used.
    /// Check <see cref="DistanceFunction"/> for available values.
    /// </param>
    /// <param name="indexKind">The index kind to use.</param>
    /// <returns>A vector store collection configured for ingested chunk records.</returns>
    /// <remarks>
    /// <para>
    /// Use the non-generic <see cref="GetIngestionRecordCollection(VectorStore, string, int, string?, string?)"/>
    /// overload for the common case where no custom metadata is needed.
    /// </para>
    /// <para>
    /// If you need custom metadata, create a type derived from <see cref="IngestionChunkVectorRecord"/>
    /// with additional properties annotated with <see cref="VectorStoreDataAttribute"/>, and pass it as the
    /// <typeparamref name="TRecord"/> type parameter. You will also need to create a derived
    /// <see cref="VectorStoreWriter{TRecord}"/> and override
    /// <see cref="VectorStoreWriter{TRecord}.SetMetadata(TRecord, string, object?)"/>
    /// to map metadata keys to typed properties.
    /// </para>
    /// <para>
    /// If you need full control over the collection schema (for example, to map to a pre-existing collection
    /// with different storage names), create a <see cref="VectorStoreCollectionDefinition"/> manually
    /// and call <see cref="VectorStore.GetCollection{TKey, TRecord}(string, VectorStoreCollectionDefinition?)"/> directly.
    /// </para>
    /// </remarks>
    [RequiresDynamicCode("This API is not compatible with NativeAOT. You can implement your own IngestionChunkWriter that uses dynamic mapping via VectorStore.GetCollectionDynamic().")]
    [RequiresUnreferencedCode("This API is not compatible with trimming. You can implement your own IngestionChunkWriter that uses dynamic mapping via VectorStore.GetCollectionDynamic().")]
    public static VectorStoreCollection<Guid, TRecord> GetIngestionRecordCollection<TRecord>(this VectorStore vectorStore,
        string collectionName, int dimensionCount, string? distanceFunction = null, string? indexKind = null)
        where TRecord : IngestionChunkVectorRecord, new()
    {
        _ = Shared.Diagnostics.Throw.IfNull(vectorStore);
        _ = Shared.Diagnostics.Throw.IfNullOrEmpty(collectionName);
        _ = Shared.Diagnostics.Throw.IfLessThanOrEqual(dimensionCount, 0);

        VectorStoreCollectionDefinition additiveDefinition = new()
        {
            Properties =
            {
                new VectorStoreKeyProperty(nameof(IngestionChunkVectorRecord.Key), typeof(Guid))
                {
                    IsAutoGenerated = true,
                },

                // The embedding source is AIContent produced by the chunker.
                // The vector store's embedding generator converts this content to a vector.
                new VectorStoreVectorProperty(nameof(IngestionChunkVectorRecord.Embedding), typeof(AIContent), dimensionCount)
                {
                    DistanceFunction = distanceFunction,
                    IndexKind = indexKind,
                },
            },
        };

        return vectorStore.GetCollection<Guid, TRecord>(collectionName, additiveDefinition);
    }

    private sealed class AIContentEmbeddingGeneratorAdapter<TEmbedding> : IEmbeddingGenerator<AIContent, TEmbedding>
        where TEmbedding : Embedding
    {
        private readonly IEmbeddingGenerator<string, TEmbedding> _innerGenerator;

        internal AIContentEmbeddingGeneratorAdapter(IEmbeddingGenerator<string, TEmbedding> innerGenerator)
        {
            _innerGenerator = innerGenerator;
        }

        public Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
            IEnumerable<AIContent> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<string> stringValues = values.Select(content => content is TextContent tc ? tc.Text ?? string.Empty : content.ToString() ?? string.Empty);
            return _innerGenerator.GenerateAsync(stringValues, options, cancellationToken);
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
            => _innerGenerator.GetService(serviceType, serviceKey);

        public void Dispose()
            => _innerGenerator.Dispose();
    }

    private sealed class StringEmbeddingGeneratorAdapter<TEmbedding> : IEmbeddingGenerator<string, TEmbedding>
        where TEmbedding : Embedding
    {
        private readonly IEmbeddingGenerator<AIContent, TEmbedding> _innerGenerator;

        internal StringEmbeddingGeneratorAdapter(IEmbeddingGenerator<AIContent, TEmbedding> innerGenerator)
        {
            _innerGenerator = innerGenerator;
        }

        public Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<AIContent> contentValues = values.Select(text => (AIContent)new TextContent(text));
            return _innerGenerator.GenerateAsync(contentValues, options, cancellationToken);
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
            => _innerGenerator.GetService(serviceType, serviceKey);

        public void Dispose()
            => _innerGenerator.Dispose();
    }
}
