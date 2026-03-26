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
/// When the vector dimension count is not known at compile time,
/// use the <see cref="VectorStoreExtensions.GetIngestionRecordCollection{TRecord, TChunk}(VectorStore, string, int, string?, string?)"/>
/// helper to create a <see cref="VectorStoreCollection{TKey, TRecord}"/> and pass it to the <see cref="VectorStoreWriter{TChunk, TRecord}"/> constructor.
/// When the vector dimension count is known at compile time, derive from this class and add
/// the <see cref="VectorStoreVectorAttribute"/> to the <see cref="Embedding"/> property.
/// </remarks>
public class IngestionChunkVectorRecord<TChunk>
{
    /// <summary>
    /// Gets or sets the unique key for this record.
    /// </summary>
    [VectorStoreKey]
    public virtual Guid Key { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the document from which this chunk was extracted.
    /// </summary>
    [VectorStoreData]
    public virtual string DocumentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content of the chunk.
    /// </summary>
    [VectorStoreData]
    public virtual TChunk? Content { get; set; }

    /// <summary>
    /// Gets or sets additional context for the chunk.
    /// </summary>
    [VectorStoreData]
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
}
