// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DataRetrieval;

/// <summary>
/// Defines the contract for a retrieval pipeline that processes queries and returns results.
/// </summary>
/// <remarks>
/// Enables DI registration and testability. Consumers depend on <see cref="IRetriever"/>
/// rather than a concrete pipeline implementation, allowing mocking in tests and
/// swappable retrieval strategies.
/// </remarks>
public interface IRetriever
{
    /// <summary>
    /// Retrieves results for the specified query.
    /// </summary>
    /// <param name="query">The user query.</param>
    /// <param name="topK">Maximum number of results to retrieve.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The retrieval results.</returns>
    Task<RetrievalResults> RetrieveAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default);
}
