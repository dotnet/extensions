// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;
using static Microsoft.Extensions.DataIngestion.DiagnosticsConstants;

namespace Microsoft.Extensions.DataIngestion;

#pragma warning disable IDE0058 // Expression value is never used
#pragma warning disable IDE0063 // Use simple 'using' statement
#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

/// <summary>
/// Represents a pipeline for ingesting data from documents and processing it into chunks.
/// </summary>
/// <typeparam name="T">The type of the chunk content.</typeparam>
public sealed class IngestionPipeline<T> : IDisposable
{
    private readonly IngestionChunker<T> _chunker;
    private readonly IngestionChunkWriter<T> _writer;
    private readonly ActivitySource _activitySource;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionPipeline{T}"/> class.
    /// </summary>
    /// <param name="chunker">The chunker to split documents into chunks.</param>
    /// <param name="writer">The writer for processing chunks.</param>
    /// <param name="options">The options for the ingestion pipeline.</param>
    /// <param name="loggerFactory">The logger factory for creating loggers.</param>
    public IngestionPipeline(
        IngestionChunker<T> chunker,
        IngestionChunkWriter<T> writer,
        IngestionPipelineOptions? options = default,
        ILoggerFactory? loggerFactory = default)
    {
        _chunker = Throw.IfNull(chunker);
        _writer = Throw.IfNull(writer);
        _activitySource = new((options ?? new()).ActivitySourceName);
        _logger = loggerFactory?.CreateLogger<IngestionPipeline<T>>();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _writer.Dispose();
        _activitySource.Dispose();
    }

    /// <summary>
    /// Gets the document processors in the pipeline.
    /// </summary>
    public IList<IngestionDocumentProcessor> DocumentProcessors { get; } = [];

    /// <summary>
    /// Gets the chunk processors in the pipeline.
    /// </summary>
    public IList<IngestionChunkProcessor<T>> ChunkProcessors { get; } = [];

    /// <summary>
    /// Processes the specified documents.
    /// </summary>
    /// <param name="documents">The asynchronous sequence of documents to process.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>An asynchronous sequence of <see cref="IngestionResult"/> instances.</returns>
    public async IAsyncEnumerable<IngestionResult> ProcessAsync(
        IAsyncEnumerable<IngestionDocument> documents,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Throw.IfNull(documents);

        await using IAsyncEnumerator<IngestionDocument> enumerator = documents.GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            IngestionDocument? document = null;
            Exception? fetchException = null;

            try
            {
                if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    break;
                }

                document = enumerator.Current;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                fetchException = ex;
            }

            using (Activity? processDocumentActivity = _activitySource.StartActivity(ProcessDocument.ActivityName))
            {
                if (fetchException is not null)
                {
                    TraceException(processDocumentActivity, fetchException);
                    _logger?.IngestingFailed(fetchException, "unknown");
                    yield return new IngestionResult("unknown", null, fetchException);
                    yield break; // Enumerator is in a faulted state and cannot produce more items
                }

                processDocumentActivity?.SetTag(ProcessSource.DocumentIdTagName, document!.Identifier);

                IngestionDocument? processedDocument = null;
                Exception? ingestException = null;
                try
                {
                    processedDocument = await IngestAsync(document!, processDocumentActivity, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    TraceException(processDocumentActivity, ex);
                    _logger?.IngestingFailed(ex, document!.Identifier);
                    ingestException = ex;
                }

                yield return new IngestionResult(document!.Identifier, processedDocument, ingestException);
            }
        }
    }

    private static string GetShortName(object any) => any.GetType().Name;

    private static void TraceException(Activity? activity, Exception ex)
    {
        activity?.SetTag(ErrorTypeTagName, ex.GetType().FullName)
                 .SetStatus(ActivityStatusCode.Error, ex.Message);
    }

    private async Task<IngestionDocument> IngestAsync(IngestionDocument document, Activity? parentActivity, CancellationToken cancellationToken)
    {
        foreach (IngestionDocumentProcessor processor in DocumentProcessors)
        {
            document = await processor.ProcessAsync(document, cancellationToken).ConfigureAwait(false);

            // A DocumentProcessor might change the document identifier (for example by extracting it from its content), so update the ID tag.
            parentActivity?.SetTag(ProcessSource.DocumentIdTagName, document.Identifier);
        }

        IAsyncEnumerable<IngestionChunk<T>> chunks = _chunker.ProcessAsync(document, cancellationToken);
        foreach (var processor in ChunkProcessors)
        {
            chunks = processor.ProcessAsync(chunks, cancellationToken);
        }

        _logger?.WritingChunks(GetShortName(_writer));
        await _writer.WriteAsync(chunks, cancellationToken).ConfigureAwait(false);
        _logger?.WroteChunks(document.Identifier);

        return document;
    }
}
