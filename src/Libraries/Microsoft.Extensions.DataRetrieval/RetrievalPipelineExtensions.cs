// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.VectorData;

namespace Microsoft.Extensions.DataRetrieval;

/// <summary>
/// Extension methods for <see cref="RetrievalPipeline"/>.
/// </summary>
public static class RetrievalPipelineExtensions
{
    /// <summary>
    /// Creates an <see cref="IRetriever"/> that binds this pipeline to a specific vector store collection.
    /// </summary>
    /// <typeparam name="TKey">The vector store key type.</typeparam>
    /// <typeparam name="TRecord">The vector store record type.</typeparam>
    /// <param name="pipeline">The retrieval pipeline.</param>
    /// <param name="collection">The vector store collection to search.</param>
    /// <param name="contentSelector">Optional function to extract text content from a record.</param>
    /// <returns>An <see cref="IRetriever"/> that processes queries through this pipeline against the specified collection.</returns>
    /// <remarks>
    /// This is symmetric with <c>IngestionPipeline.ProcessAsync</c> — both pipelines are engines that
    /// operate on a data source. <c>AsRetriever</c> creates a ready-to-use endpoint from the engine.
    /// <code>
    /// var pipeline = new RetrievalPipeline(loggerFactory: loggerFactory);
    /// pipeline.QueryProcessors.Add(new MultiQueryExpander(chatClient));
    /// pipeline.ResultProcessors.Add(new LlmReranker(chatClient));
    ///
    /// IRetriever retriever = pipeline.AsRetriever(collection, r =&gt; r.Content);
    /// var results = await retriever.RetrieveAsync("What are the retention policies?");
    /// </code>
    /// </remarks>
    public static IRetriever AsRetriever<TKey, TRecord>(
        this RetrievalPipeline pipeline,
        VectorStoreCollection<TKey, TRecord> collection,
        Func<TRecord, string>? contentSelector = null)
        where TKey : notnull
        where TRecord : class
    {
        return new VectorStoreRetriever<TKey, TRecord>(pipeline, collection, contentSelector);
    }
}
