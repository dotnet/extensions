// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DataRetrieval;

/// <summary>
/// Processes retrieval results after vector search is performed.
/// </summary>
/// <remarks>
/// Post-search processors transform or filter <see cref="RetrievalResults"/>
/// after they are returned from the vector store. Examples include re-ranking,
/// CRAG (Corrective RAG) quality validation, and deduplication.
/// </remarks>
public abstract class RetrievalResultProcessor
{
    /// <summary>
    /// Processes the results asynchronously after vector search.
    /// </summary>
    /// <param name="results">The retrieval results to process.</param>
    /// <param name="query">The original query (for context during processing).</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The processed results.</returns>
    public abstract Task<RetrievalResults> ProcessAsync(RetrievalResults results, RetrievalQuery query, CancellationToken cancellationToken = default);
}
