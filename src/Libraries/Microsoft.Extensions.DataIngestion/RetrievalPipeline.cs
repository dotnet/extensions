// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.Shared.Diagnostics;
using static Microsoft.Extensions.DataIngestion.DiagnosticsConstants;

namespace Microsoft.Extensions.DataIngestion;

#pragma warning disable IDE0058 // Expression value is never used
#pragma warning disable IDE0063 // Use simple 'using' statement

/// <summary>
/// Represents a pipeline for retrieving data from a vector store with pre- and post-processing.
/// </summary>
/// <remarks>
/// Mirrors <see cref="IngestionPipeline{T}"/> design.
/// Flow: query → <see cref="QueryProcessors"/> → vector search → <see cref="ResultProcessors"/> → results.
/// With an empty processor list, this behaves identically to a raw
/// <see cref="VectorStoreCollection{TKey, TRecord}.SearchAsync"/> call.
/// </remarks>
public sealed class RetrievalPipeline : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetrievalPipeline"/> class.
    /// </summary>
    /// <param name="options">The options for the retrieval pipeline.</param>
    /// <param name="loggerFactory">The logger factory for creating loggers.</param>
    public RetrievalPipeline(
        RetrievalPipelineOptions? options = null,
        ILoggerFactory? loggerFactory = null)
    {
        _activitySource = new((options ?? new()).ActivitySourceName);
        _logger = loggerFactory?.CreateLogger<RetrievalPipeline>();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _activitySource.Dispose();
    }

    /// <summary>
    /// Gets the pre-search query processors (e.g., query expansion, HyDE).
    /// </summary>
    public IList<RetrievalQueryProcessor> QueryProcessors { get; } = [];

    /// <summary>
    /// Gets the post-search result processors (e.g., re-ranking, CRAG).
    /// </summary>
    public IList<RetrievalResultProcessor> ResultProcessors { get; } = [];

    /// <summary>
    /// Executes the retrieval pipeline: query processing → vector search → result processing.
    /// </summary>
    /// <typeparam name="TKey">The vector store key type.</typeparam>
    /// <typeparam name="TRecord">The vector store record type.</typeparam>
    /// <param name="collection">The vector store collection to search.</param>
    /// <param name="query">The user query.</param>
    /// <param name="topK">Maximum results to retrieve per search variant.</param>
    /// <param name="contentSelector">Extracts text content from a record for result processing.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The retrieval results.</returns>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075",
        Justification = "Record properties are accessed for diagnostic metadata population only.")]
    public async Task<RetrievalResults> RetrieveAsync<TKey, TRecord>(
        VectorStoreCollection<TKey, TRecord> collection,
        string query,
        int topK = 5,
        Func<TRecord, string>? contentSelector = null,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        where TRecord : class
    {
        Throw.IfNull(collection);
        Throw.IfNullOrEmpty(query);

        using (Activity? rootActivity = _activitySource.StartActivity("RetrievalPipeline.Retrieve"))
        {
            rootActivity?.SetTag("rag.query", query)
                         .SetTag("rag.topK", topK);

            // Phase 1: Pre-query processing
            var retrievalQuery = new RetrievalQuery(query);

            foreach (var processor in QueryProcessors)
            {
                _logger?.RunningQueryProcessor(processor.GetType().Name);
                retrievalQuery = await processor.ProcessAsync(retrievalQuery, cancellationToken).ConfigureAwait(false);
            }

            rootActivity?.SetTag("rag.query.variants", retrievalQuery.Variants.Count);

            // Phase 2: Vector search (one search per variant, merge with RRF)
            var allChunks = new List<RetrievalChunk>();

            foreach (string variant in retrievalQuery.Variants)
            {
                _logger?.SearchingVariant(variant.Length > 80 ? variant[..80] : variant);
                var searchResults = collection.SearchAsync(variant, top: topK, cancellationToken: cancellationToken);

                await foreach (var result in searchResults.ConfigureAwait(false))
                {
                    string content = contentSelector is not null && result.Record is not null
                        ? contentSelector(result.Record)
                        : result.Record?.ToString() ?? string.Empty;

                    var chunk = new RetrievalChunk(content, result.Score ?? 0.0);

                    // Populate record dictionary for downstream reconstruction
                    if (result.Record is not null)
                    {
                        foreach (var prop in result.Record.GetType().GetProperties())
                        {
                            chunk.Record[prop.Name] = prop.GetValue(result.Record);
                        }
                    }

                    allChunks.Add(chunk);
                }
            }

            // Deduplicate by content if multiple variants returned overlapping results
            if (retrievalQuery.Variants.Count > 1)
            {
                allChunks = DeduplicateWithRrf(allChunks);
            }

            var results = new RetrievalResults { Chunks = allChunks };
            rootActivity?.SetTag("rag.results.count", results.Chunks.Count);

            // Phase 3: Post-search result processing
            foreach (var processor in ResultProcessors)
            {
                _logger?.RunningResultProcessor(processor.GetType().Name);
                results = await processor.ProcessAsync(results, retrievalQuery, cancellationToken).ConfigureAwait(false);
            }

            rootActivity?.SetTag("rag.results.final_count", results.Chunks.Count);
            return results;
        }
    }

    /// <summary>
    /// Deduplicates chunks using Reciprocal Rank Fusion across multiple query variants.
    /// </summary>
    private static List<RetrievalChunk> DeduplicateWithRrf(List<RetrievalChunk> chunks)
    {
        const int RrfK = 60;

        var grouped = chunks
            .GroupBy(c => c.Content)
            .Select(g => new
            {
                Chunk = g.First(),
                RrfScore = g.Sum(c =>
                {
                    int rank = chunks.IndexOf(c) + 1;
                    return 1.0 / (RrfK + rank);
                })
            })
            .OrderByDescending(x => x.RrfScore)
            .ToList();

        foreach (var item in grouped)
        {
            item.Chunk.Score = item.RrfScore;
        }

        return grouped.Select(x => x.Chunk).ToList();
    }
}
