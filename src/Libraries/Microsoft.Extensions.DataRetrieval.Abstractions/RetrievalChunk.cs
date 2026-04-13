// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.DataRetrieval;

/// <summary>
/// Represents a single chunk returned from retrieval.
/// </summary>
public sealed class RetrievalChunk
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RetrievalChunk"/> class.
    /// </summary>
    /// <param name="content">The text content of the chunk.</param>
    /// <param name="score">The relevance score from vector search.</param>
    public RetrievalChunk(string content, double score)
    {
        Content = content;
        Score = score;
    }

    /// <summary>
    /// Gets the text content of this chunk.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets or sets the relevance score.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets the underlying record data as key-value pairs.
    /// </summary>
    /// <remarks>
    /// Contains the full record fields from the vector store, enabling
    /// downstream consumers to reconstruct strongly-typed records.
    /// </remarks>
    public IDictionary<string, object?> Record { get; } = new Dictionary<string, object?>();
}
