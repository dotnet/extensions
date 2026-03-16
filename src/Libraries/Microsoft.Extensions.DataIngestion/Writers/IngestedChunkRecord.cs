// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.VectorData;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Represents the base record type used by <see cref="VectorStoreWriter{TChunk, TRecord}"/> to store ingested chunks in a vector store.
/// </summary>
/// <typeparam name="TChunk">The type of the chunk content.</typeparam>
/// <remarks>
/// When the vector dimension count is not known at compile time, use the <see cref="CreateCollectionDefinition"/>
/// helper to create a <see cref="VectorStoreCollectionDefinition"/> and pass it to the vector store collection constructor.
/// When the vector dimension count is known at compile time, derive from this class and add
/// the <see cref="VectorStoreVectorAttribute"/> to the <see cref="Embedding"/> property.
/// </remarks>
public class IngestedChunkRecord<TChunk>
{
    /// <summary>
    /// The storage name for the <see cref="Key"/> property.
    /// </summary>
    protected const string KeyPropertyName = "key";

    /// <summary>
    /// The storage name for the <see cref="DocumentId"/> property.
    /// </summary>
    protected const string DocumentIdPropertyName = "documentid";

    /// <summary>
    /// The storage name for the <see cref="Content"/> property.
    /// </summary>
    protected const string ContentPropertyName = "content";

    /// <summary>
    /// The storage name for the <see cref="Context"/> property.
    /// </summary>
    protected const string ContextPropertyName = "context";

    /// <summary>
    /// The storage name for the <see cref="Embedding"/> property.
    /// </summary>
    protected const string EmbeddingPropertyName = "embedding";

    /// <summary>
    /// Creates a <see cref="VectorStoreCollectionDefinition"/> for <see cref="IngestedChunkRecord{TChunk}"/>.
    /// </summary>
    /// <param name="dimensionCount">The number of dimensions that the vector has.</param>
    /// <param name="distanceFunction">
    /// The distance function to use. When not provided, the default specific to given database will be used.
    /// Check <see cref="DistanceFunction"/> for available values.
    /// </param>
    /// <param name="indexKind">The index kind to use.</param>
    /// <returns>A <see cref="VectorStoreCollectionDefinition"/> suitable for creating a vector store collection.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dimensionCount"/> is less than or equal to zero.</exception>
#pragma warning disable CA1000 // Do not declare static members on generic types - needs access to TChunk type parameter
    public static VectorStoreCollectionDefinition CreateCollectionDefinition(int dimensionCount, string? distanceFunction = null, string? indexKind = null)
#pragma warning restore CA1000
    {
        _ = Shared.Diagnostics.Throw.IfLessThanOrEqual(dimensionCount, 0);

        return new VectorStoreCollectionDefinition
        {
            Properties =
            {
                new VectorStoreKeyProperty(nameof(Key), typeof(Guid)) { StorageName = KeyPropertyName },

                // By using TChunk as the type here we allow the vector store
                // to handle the conversion from TChunk to the actual vector type it supports.
                new VectorStoreVectorProperty(nameof(Embedding), typeof(TChunk), dimensionCount)
                {
                    StorageName = EmbeddingPropertyName,
                    DistanceFunction = distanceFunction,
                    IndexKind = indexKind,
                },
                new VectorStoreDataProperty(nameof(Content), typeof(TChunk)) { StorageName = ContentPropertyName },
                new VectorStoreDataProperty(nameof(Context), typeof(string)) { StorageName = ContextPropertyName },
                new VectorStoreDataProperty(nameof(DocumentId), typeof(string))
                {
                    StorageName = DocumentIdPropertyName,
                    IsIndexed = true,
                },
            },
        };
    }

    /// <summary>
    /// Gets or sets the unique key for this record.
    /// </summary>
    [VectorStoreKey(StorageName = KeyPropertyName)]
    public virtual Guid Key { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the document from which this chunk was extracted.
    /// </summary>
    [VectorStoreData(StorageName = DocumentIdPropertyName)]
    public virtual string DocumentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content of the chunk.
    /// </summary>
    [VectorStoreData(StorageName = ContentPropertyName)]
    public virtual TChunk? Content { get; set; }

    /// <summary>
    /// Gets or sets additional context for the chunk.
    /// </summary>
    [VectorStoreData(StorageName = ContextPropertyName)]
    public virtual string? Context { get; set; }

    /// <summary>
    /// Gets the embedding value for this record.
    /// </summary>
    /// <remarks>
    /// By default, returns the <see cref="Content"/> value. The vector store's embedding generator
    /// will convert this to a vector. Override this property in derived classes to add
    /// the <see cref="VectorStoreVectorAttribute"/> with the appropriate dimension count.
    /// </remarks>
    public virtual TChunk? Embedding => Content;

    /// <summary>
    /// Sets a metadata value on the record.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <remarks>
    /// Override this method in derived classes to store metadata as typed properties with
    /// <see cref="VectorStoreDataAttribute"/> attributes.
    /// </remarks>
    public virtual void SetMetadata(string key, object? value)
    {
        throw new NotSupportedException($"Metadata key '{key}' is not supported. Override {nameof(SetMetadata)} in a derived class to handle metadata.");
    }
}
