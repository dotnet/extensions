// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DataRetrieval;

/// <summary>
/// Defines a re-ranking strategy for retrieval results.
/// </summary>
/// <remarks>
/// Re-rankers score and reorder retrieval chunks based on relevance to the query.
/// Implementations may use LLM-based scoring, cross-encoder models (e.g., ONNX),
/// or other ranking strategies.
/// </remarks>
public interface IReranker
{
    /// <summary>
    /// Re-ranks the provided chunks based on their relevance to the query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="chunks">The chunks to re-rank.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The re-ranked chunks, ordered by relevance (highest first).</returns>
    Task<IReadOnlyList<RetrievalChunk>> RerankAsync(string query, IReadOnlyList<RetrievalChunk> chunks, CancellationToken cancellationToken = default);
}
