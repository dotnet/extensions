// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.DataRetrieval;

/// <summary>
/// Represents the results of a retrieval operation.
/// </summary>
public sealed class RetrievalResults
{
    /// <summary>
    /// Gets or sets the retrieved chunks, ordered by relevance.
    /// </summary>
    public IList<RetrievalChunk> Chunks { get; set; } = [];

    /// <summary>
    /// Gets the metadata from the retrieval pipeline.
    /// </summary>
    /// <remarks>
    /// Pipeline processors may add metadata such as CRAG quality scores,
    /// reranking diagnostics, or query expansion details.
    /// </remarks>
    public IDictionary<string, object?> Metadata { get; } = new Dictionary<string, object?>();
}
