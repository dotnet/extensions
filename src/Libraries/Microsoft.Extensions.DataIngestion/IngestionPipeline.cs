// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
/// <typeparam name="T">The type of the chunk content.</typeparam>
public sealed class IngestionPipeline<T> : IDisposable
{
    private readonly IngestionDocumentReader _reader;
    private readonly IngestionChunker<T> _chunker;
    private readonly IngestionChunkWriter<T> _writer;
    private readonly ActivitySource _activitySource;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionPipeline{T}"/> class.
    /// </summary>
    /// <param name="reader">The reader for ingestion documents.</param>
    /// <param name="chunker">The chunker to split documents into chunks.</param>
    /// <param name="writer">The writer for processing chunks.</param>
    /// <param name="options">The options for the ingestion pipeline.</param>
    /// <param name="loggerFactory">The logger factory for creating loggers.</param>
    public IngestionPipeline(
        IngestionDocumentReader reader,
        IngestionChunker<T> chunker,
        IngestionChunkWriter<T> writer,
        IngestionPipelineOptions? options = default,
        ILoggerFactory? loggerFactory = default)
    {
        _reader = Throw.IfNull(reader);
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
    /// Processes all files in the specified directory that match the given search pattern and option.
    /// </summary>
    /// <param name="directory">The directory to process.</param>
    /// <param name="searchPattern">The search pattern for file selection.</param>
    /// <param name="searchOption">The search option for directory traversal.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async IAsyncEnumerable<IngestionResult> ProcessAsync(DirectoryInfo directory, string searchPattern = "*.*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Throw.IfNull(directory);
        Throw.IfNullOrEmpty(searchPattern);
        Throw.IfOutOfRange((int)searchOption, (int)SearchOption.TopDirectoryOnly, (int)SearchOption.AllDirectories);

        using (Activity? rootActivity = _activitySource.StartActivity(ProcessDirectory.ActivityName))
        {
            rootActivity?.SetTag(ProcessDirectory.DirectoryPathTagName, directory.FullName)
                         .SetTag(ProcessDirectory.SearchPatternTagName, searchPattern)
                         .SetTag(ProcessDirectory.SearchOptionTagName, searchOption.ToString());
            _logger?.ProcessingDirectory(directory.FullName, searchPattern, searchOption);

            await foreach (var ingestionResult in ProcessAsync(directory.EnumerateFiles(searchPattern, searchOption), rootActivity, cancellationToken).ConfigureAwait(false))
            {
                yield return ingestionResult;
            }
        }
    }

    /// <summary>
    /// Processes the specified files.
    /// </summary>
    /// <param name="files">The collection of files to process.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async IAsyncEnumerable<IngestionResult> ProcessAsync(IEnumerable<FileInfo> files, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Throw.IfNull(files);

        using (Activity? rootActivity = _activitySource.StartActivity(ProcessFiles.ActivityName))
        {
            await foreach (var ingestionResult in ProcessAsync(files, rootActivity, cancellationToken).ConfigureAwait(false))
            {
                yield return ingestionResult;
            }
        }
    }

    /// <summary>
    /// Processes a document from the specified stream.
    /// </summary>
    /// <param name="documentSource">The stream containing the document data to be processed. Must be readable and positioned at the start of the
    /// document.</param>
    /// <param name="documentIdentifier">A unique identifier for the document being processed. Cannot be null or empty.</param>
    /// <param name="documentMediaType">The media type (MIME type) of the document, such as "application/pdf" or "text/plain".</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous processing operation.</returns>
    public async Task ProcessAsync(Stream documentSource, string documentIdentifier, string documentMediaType, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(documentSource);
        Throw.IfNullOrEmpty(documentIdentifier);

        // We don't validate documentMediaType for null/empty here, as some readers may not require it.

        using (Activity? processDocument = _activitySource.StartActivity(ProcessStream.ActivityName, ActivityKind.Internal, parentContext: default))
        {
            processDocument?.SetTag(ProcessSource.DocumentIdTagName, documentIdentifier);
            processDocument?.SetTag(ProcessStream.DocumentMediaType, documentMediaType);

            IngestionDocument? document = null;
            try
            {
                document = await _reader.ReadAsync(documentSource, documentIdentifier, documentMediaType, cancellationToken).ConfigureAwait(false);

                processDocument?.SetTag(ProcessSource.DocumentIdTagName, document.Identifier);
                _logger?.ReadDocument(document.Identifier);

                document = await IngestAsync(document, processDocument, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                TraceException(processDocument, ex);

                _logger?.IngestingFailed(ex, document?.Identifier ?? documentIdentifier);

                // Contrary to other processing methods that take collections of files, here we re-throw the exception to inform the caller.
                throw;
            }
        }
    }

    /// <summary>
    /// Processes a document from the specified buffer.
    /// </summary>
    /// <param name="documentContent">The buffer containing the document data to be processed.</param>
    /// <param name="documentIdentifier">A unique identifier for the document being processed. Cannot be null or empty.</param>
    /// <param name="documentMediaType">The media type (MIME type) of the document, such as "application/pdf" or "text/plain".</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous processing operation.</returns>
    public async Task ProcessAsync(ReadOnlyMemory<byte> documentContent, string documentIdentifier, string documentMediaType, CancellationToken cancellationToken = default)
    {
        if (documentContent.IsEmpty)
        {
            Throw.ArgumentException("Document content cannot be empty.", nameof(documentContent));
        }

        Throw.IfNullOrEmpty(documentIdentifier);

        using MemoryStream documentStream = MemoryMarshal.TryGetArray(documentContent, out ArraySegment<byte> array)
            ? new MemoryStream(array.Array!, array.Offset, array.Count)
            : new MemoryStream(documentContent.ToArray());

        await ProcessAsync(documentStream, documentIdentifier, documentMediaType, cancellationToken).ConfigureAwait(false);
    }

    private static string GetShortName(object any) => any.GetType().Name;

    private static void TraceException(Activity? activity, Exception ex)
    {
        activity?.SetTag(ErrorTypeTagName, ex.GetType().FullName)
                 .SetStatus(ActivityStatusCode.Error, ex.Message);
    }

    private async IAsyncEnumerable<IngestionResult> ProcessAsync(IEnumerable<FileInfo> files, Activity? rootActivity,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
#if NET
        if (System.Linq.Enumerable.TryGetNonEnumeratedCount(files, out int count))
#else
        if (files is IReadOnlyCollection<FileInfo> { Count: int count })
#endif
        {
            rootActivity?.SetTag(ProcessFiles.FileCountTagName, count);
            _logger?.LogFileCount(count);
        }

        foreach (FileInfo fileInfo in files)
        {
            using (Activity? processFileActivity = _activitySource.StartActivity(ProcessFile.ActivityName, ActivityKind.Internal, parentContext: rootActivity?.Context ?? default))
            {
                processFileActivity?.SetTag(ProcessFile.FilePathTagName, fileInfo.FullName);
                _logger?.ReadingFile(fileInfo.FullName, GetShortName(_reader));

                IngestionDocument? document = null;
                Exception? failure = null;
                try
                {
                    document = await _reader.ReadAsync(fileInfo, cancellationToken).ConfigureAwait(false);

                    processFileActivity?.SetTag(ProcessSource.DocumentIdTagName, document.Identifier);
                    _logger?.ReadDocument(document.Identifier);

                    document = await IngestAsync(document, processFileActivity, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    TraceException(processFileActivity, ex);
                    _logger?.IngestingFailed(ex, document?.Identifier ?? fileInfo.FullName);

                    failure = ex;
                }

                string documentId = document?.Identifier ?? fileInfo.FullName;
                yield return new IngestionResult(documentId, document, failure);
            }
        }
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
