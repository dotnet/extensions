// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Represents options for the <see cref="VectorStoreWriter{T}"/>.
/// </summary>
public sealed class VectorStoreWriterOptions
{
    /// <summary>
    /// The default number of tokens used to represent a single batch size.
    /// </summary>
    /// <remarks>
    /// This is calculated as 256 times the default tokens per chunk (2000), resulting in 512,000 tokens.
    /// </remarks>
    private const int DefaultBatchTokenCount = 256 * 2000; // 256 * IngestionChunkerOptions.DefaultTokensPerChunk

    /// <summary>
    /// Gets or sets the name of the collection. When not provided, "chunks" will be used.
    /// </summary>
    public string CollectionName
    {
        get => field ?? "chunks";
        set => field = Throw.IfNullOrEmpty(value);
    }

    /// <summary>
    /// Gets or sets the distance function to use when creating the collection.
    /// </summary>
    /// <remarks>
    /// When not provided, the default specific to given database will be used. Check <see cref="VectorData.DistanceFunction"/> for available values.
    /// </remarks>
    public string? DistanceFunction { get; set; }

    /// <summary>
    /// Gets or sets the index kind to use when creating the collection.
    /// </summary>
    public string? IndexKind { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to perform incremental ingestion.
    /// </summary>
    /// <remarks>
    /// When enabled, the writer will delete the chunks for the given document after inserting the new ones.
    /// Effectively the ingestion will "replace" the existing chunks for the document with the new ones.
    /// </remarks>
    public bool IncrementalIngestion { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of tokens used to represent a single batch size.
    /// </summary>
    /// <remarks>
    /// The writer accumulates chunks until the total number of tokens reaches this limit,
    /// then performs a batch upsert operation. Default is 512,000 tokens (256 * 2000).
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Value is less than or equal to zero.
    /// </exception>
    public int BatchTokenCount
    {
        get => field == default ? DefaultBatchTokenCount : field;
        set => field = Throw.IfLessThanOrEqual(value, 0);
    }
}
