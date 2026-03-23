// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.VectorData;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Provides extension methods for working with vector stores in the context of data ingestion.
/// </summary>
public static class VectorStoreExtensions
{
    internal const string EmbeddingStorageName = "embedding";

    /// <summary>
    /// Provides a convenient method to get a vector store collection specifically designed for storing ingested chunk records
    /// using the default <see cref="IngestionChunkVectorRecord{TChunk}"/> type with <see langword="string"/> content.
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
    public static VectorStoreCollection<Guid, IngestionChunkVectorRecord<string>> GetIngestionRecordCollection(this VectorStore vectorStore,
        string collectionName, int dimensionCount, string? distanceFunction = null, string? indexKind = null)
    {
        return vectorStore.GetIngestionRecordCollection<IngestionChunkVectorRecord<string>, string>(collectionName, dimensionCount, EmbeddingStorageName, distanceFunction, indexKind);
    }

    /// <summary>
    /// Provides a convenient method to get a vector store collection specifically designed for storing ingested chunk records.
    /// </summary>
    /// <typeparam name="TRecord">The type of the record to be stored in the collection.</typeparam>
    /// <typeparam name="TChunk">The type of the chunk content.</typeparam>
    /// <param name="vectorStore">The vector store instance to create the collection in.</param>
    /// <param name="collectionName">The name of the collection to be created.</param>
    /// <param name="dimensionCount">The number of dimensions that the vector has.</param>
    /// <param name="storageName">The storage name for the vector property.</param>
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
    /// If you need custom metadata, create a type derived from <see cref="IngestionChunkVectorRecord{TChunk}"/>
    /// with additional properties annotated with <see cref="VectorStoreDataAttribute"/>, and pass it as the
    /// <typeparamref name="TRecord"/> type parameter. You will also need to create a derived
    /// <see cref="VectorStoreWriter{TChunk, TRecord}"/> and override
    /// <see cref="VectorStoreWriter{TChunk, TRecord}.SetMetadata(TRecord, string, object?)"/>
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
    public static VectorStoreCollection<Guid, TRecord> GetIngestionRecordCollection<TRecord, TChunk>(this VectorStore vectorStore,
        string collectionName, int dimensionCount, string storageName, string? distanceFunction = null, string? indexKind = null)
        where TRecord : IngestionChunkVectorRecord<TChunk>, new()
    {
        _ = Shared.Diagnostics.Throw.IfNull(vectorStore);
        _ = Shared.Diagnostics.Throw.IfNullOrEmpty(collectionName);
        _ = Shared.Diagnostics.Throw.IfLessThanOrEqual(dimensionCount, 0);
        _ = Shared.Diagnostics.Throw.IfNullOrEmpty(storageName);

        VectorStoreCollectionDefinition additiveDefinition = new()
        {
            Properties =
            {
                // By using TChunk as the type here we allow the vector store
                // to handle the conversion from TChunk to the actual vector type it supports.
                new VectorStoreVectorProperty(nameof(IngestionChunkVectorRecord<>.Embedding), typeof(TChunk), dimensionCount)
                {
                    StorageName = storageName,
                    DistanceFunction = distanceFunction,
                    IndexKind = indexKind,
                },
            },
        };

        return vectorStore.GetCollection<Guid, TRecord>(collectionName, additiveDefinition);
    }
}
