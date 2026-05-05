// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.VectorData;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataRetrieval;

/// <summary>
/// An <see cref="IRetriever"/> implementation that retrieves results from a
/// <see cref="VectorStoreCollection{TKey, TRecord}"/> using a <see cref="RetrievalPipeline"/>.
/// </summary>
/// <typeparam name="TKey">The vector store key type.</typeparam>
/// <typeparam name="TRecord">The vector store record type.</typeparam>
/// <remarks>
/// Register via DI to enable constructor injection of <see cref="IRetriever"/>:
/// <code>
/// services.AddSingleton&lt;IRetriever&gt;(sp =&gt;
///     new VectorStoreRetriever&lt;string, MyRecord&gt;(
///         sp.GetRequiredService&lt;RetrievalPipeline&gt;(),
///         sp.GetRequiredService&lt;VectorStoreCollection&lt;string, MyRecord&gt;&gt;(),
///         record =&gt; record.Content));
/// </code>
/// </remarks>
public sealed class VectorStoreRetriever<TKey, TRecord> : IRetriever
    where TKey : notnull
    where TRecord : class
{
    private readonly RetrievalPipeline _pipeline;
    private readonly VectorStoreCollection<TKey, TRecord> _collection;
    private readonly Func<TRecord, string>? _contentSelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="VectorStoreRetriever{TKey, TRecord}"/> class.
    /// </summary>
    /// <param name="pipeline">The retrieval pipeline to use.</param>
    /// <param name="collection">The vector store collection to search.</param>
    /// <param name="contentSelector">Optional function to extract text content from a record.</param>
    public VectorStoreRetriever(
        RetrievalPipeline pipeline,
        VectorStoreCollection<TKey, TRecord> collection,
        Func<TRecord, string>? contentSelector = null)
    {
        _pipeline = Throw.IfNull(pipeline);
        _collection = Throw.IfNull(collection);
        _contentSelector = contentSelector;
    }

    /// <inheritdoc/>
    public Task<RetrievalResults> RetrieveAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        return _pipeline.RetrieveAsync(_collection, query, topK, _contentSelector, cancellationToken);
    }
}
