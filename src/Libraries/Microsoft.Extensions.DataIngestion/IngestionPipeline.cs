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

/// <summary>
/// Represents a pipeline for ingesting data from documents and processing it into chunks.
/// </summary>
/// <typeparam name="TChunk">The type of the chunk content.</typeparam>
/// <typeparam name="TSource">The type of the source content.</typeparam>
public sealed class IngestionPipeline<TChunk, TSource> : IDisposable
{
    internal readonly ActivitySource ActivitySource;
    internal readonly ILogger? Logger;
    private readonly IIngestionDocumentReader<TSource> _reader;
    private readonly IngestionChunker<TChunk> _chunker;
    private readonly IngestionChunkWriter<TChunk> _writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionPipeline{TChunk, TSource}"/> class.
    /// </summary>
    /// <param name="reader">The reader for ingestion documents.</param>
    /// <param name="chunker">The chunker to split documents into chunks.</param>
    /// <param name="writer">The writer for processing chunks.</param>
    /// <param name="options">The options for the ingestion pipeline.</param>
    /// <param name="loggerFactory">The logger factory for creating loggers.</param>
    public IngestionPipeline(
        IIngestionDocumentReader<TSource> reader,
        IngestionChunker<TChunk> chunker,
        IngestionChunkWriter<TChunk> writer,
        IngestionPipelineOptions? options = default,
        ILoggerFactory? loggerFactory = default)
    {
        _reader = Throw.IfNull(reader);
        _chunker = Throw.IfNull(chunker);
        _writer = Throw.IfNull(writer);
        ActivitySource = new((options ?? new()).ActivitySourceName);
        Logger = loggerFactory?.CreateLogger<IngestionPipeline<TChunk, TSource>>();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _writer.Dispose();
        ActivitySource.Dispose();
    }

    /// <summary>
    /// Gets the document processors in the pipeline.
    /// </summary>
    public IList<IngestionDocumentProcessor> DocumentProcessors { get; } = [];

    /// <summary>
    /// Gets the chunk processors in the pipeline.
    /// </summary>
    public IList<IngestionChunkProcessor<TChunk>> ChunkProcessors { get; } = [];

    /// <summary>
    /// Processes the specified input.
    /// </summary>
    /// <param name="source">The source input to process.</param>
    /// <param name="documentIdentifier">The unique documentIdentifier for the document.</param>
    /// <param name="sourceMediaType">The media type of the source.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessAsync(TSource source, string documentIdentifier, string? sourceMediaType = null, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(source);
        Throw.IfNull(documentIdentifier);

        using (Activity? processActivity = ActivitySource.StartActivity(ProcessSource.ActivityName))
        {
            IngestionDocument? document = null;
            try
            {
                document = await _reader.ReadAsync(source, documentIdentifier, sourceMediaType, cancellationToken).ConfigureAwait(false);

                processActivity?.SetTag(ProcessSource.DocumentIdTagName, document.Identifier);
                Logger?.ReadDocument(document.Identifier);

                document = await IngestAsync(document, processActivity, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                TraceException(processActivity, ex);
                Logger?.IngestingFailed(ex, document?.Identifier ?? documentIdentifier);

                throw;
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

            // A DocumentProcessor might change the document documentIdentifier (for example by extracting it from its content), so update the ID tag.
            parentActivity?.SetTag(ProcessSource.DocumentIdTagName, document.Identifier);
        }

        IAsyncEnumerable<IngestionChunk<TChunk>> chunks = _chunker.ProcessAsync(document, cancellationToken);
        foreach (var processor in ChunkProcessors)
        {
            chunks = processor.ProcessAsync(chunks, cancellationToken);
        }

        Logger?.WritingChunks(GetShortName(_writer));
        await _writer.WriteAsync(chunks, cancellationToken).ConfigureAwait(false);
        Logger?.WroteChunks(document.Identifier);

        return document;
    }
}
