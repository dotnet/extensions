// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Represents options for the <see cref="VectorStoreWriter{TKey, TChunk, TRecord}"/>.
/// </summary>
public sealed class VectorStoreWriterOptions
{
    private const int DefaultBatchTokenCount = 256 * IngestionChunkerOptions.DefaultTokensPerChunk;

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
    /// then performs a batch upsert operation. Default is 256 * <see cref="IngestionChunkerOptions.MaxTokensPerChunk"/>.
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
