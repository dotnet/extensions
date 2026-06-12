// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Represents the base record type used by <see cref="VectorStoreWriter{TRecord}"/> to store ingested chunks in a vector store.
/// </summary>
/// <remarks>
/// When the vector dimension count is not known at compile time,
/// use the <see cref="VectorStoreExtensions.GetIngestionRecordCollection{TRecord}(VectorStore, string, int, string?, string?)"/>
/// helper to create a <see cref="VectorStoreCollection{TKey, TRecord}"/> and pass it to the <see cref="VectorStoreWriter{TRecord}"/> constructor.
/// When the vector dimension count is known at compile time, derive from this class and add
/// the <see cref="VectorStoreVectorAttribute"/> to the <see cref="Embedding"/> property.
/// </remarks>
public class IngestionChunkVectorRecord
{
    private AIContent? _content;

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
    /// Gets or sets the JSON-serialized content of the chunk, used for storage and retrieval.
    /// </summary>
    [VectorStoreData]
    public virtual string SerializedContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content of the chunk.
    /// </summary>
    [UnconditionalSuppressMessage("AotAnalysis", "IL3050", Justification = "DefaultJsonTypeInfoResolver is only used when reflection-based serialization is enabled")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "DefaultJsonTypeInfoResolver is only used when reflection-based serialization is enabled")]
    public virtual AIContent? Content
    {
        get
        {
            if (_content is not null)
            {
                return _content;
            }

            if (string.IsNullOrEmpty(SerializedContent))
            {
                return null;
            }

            return _content = JsonSerializer.Deserialize<AIContent>(SerializedContent, AIJsonUtilities.DefaultOptions);
        }
        set
        {
            _content = value;
            SerializedContent = JsonSerializer.Serialize(value, AIJsonUtilities.DefaultOptions);
        }
    }

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
    public virtual AIContent? Embedding => Content;
}
