// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Processes a retrieval query before vector search is performed.
/// </summary>
/// <remarks>
/// Pre-search processors transform or expand a <see cref="RetrievalQuery"/>
/// before it is sent to the vector store. Examples include multi-query expansion,
/// HyDE (Hypothetical Document Embeddings), and adaptive routing.
/// </remarks>
public abstract class RetrievalQueryProcessor
{
    /// <summary>
    /// Processes the query asynchronously before vector search.
    /// </summary>
    /// <param name="query">The retrieval query to process.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The processed query.</returns>
    public abstract Task<RetrievalQuery> ProcessAsync(RetrievalQuery query, CancellationToken cancellationToken = default);
}
