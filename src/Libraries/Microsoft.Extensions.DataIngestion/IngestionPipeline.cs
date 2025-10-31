// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    private readonly IngestionPipelineOptions _options;
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
        _options = options?.Clone() ?? new();
        _activitySource = new(_options.ActivitySourceName);
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
    public async Task ProcessAsync(DirectoryInfo directory, string searchPattern = "*.*", SearchOption searchOption = SearchOption.TopDirectoryOnly, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(directory);
        Throw.IfNullOrEmpty(searchPattern);
        Throw.IfOutOfRange((int)searchOption, (int)SearchOption.TopDirectoryOnly, (int)SearchOption.AllDirectories);

        using (Activity? rootActivity = StartActivity(ProcessDirectory.ActivityName, ActivityKind.Internal))
        {
            rootActivity?.SetTag(ProcessDirectory.DirectoryPathTagName, directory.FullName);
            rootActivity?.SetTag(ProcessDirectory.SearchPatternTagName, searchPattern);
            rootActivity?.SetTag(ProcessDirectory.SearchOptionTagName, searchOption.ToString());
            _logger?.ProcessingDirectory(directory.FullName, searchPattern, searchOption);

            await ProcessAsync(directory.EnumerateFiles(searchPattern, searchOption), rootActivity, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Processes the specified files.
    /// </summary>
    /// <param name="files">The collection of files to process.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessAsync(IEnumerable<FileInfo> files, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(files);

        using (Activity? rootActivity = StartActivity(ProcessFiles.ActivityName, ActivityKind.Internal))
        {
            await ProcessAsync(files, rootActivity, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string GetShortName(object any) => any.GetType().Name;

    private static void TraceException(Activity? activity, Exception ex)
    {
        activity?.SetTag(ErrorTypeTagName, ex.GetType().FullName);
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    }

    private async Task ProcessAsync(IEnumerable<FileInfo> files, Activity? rootActivity, CancellationToken cancellationToken)
    {
        if (files is IReadOnlyList<FileInfo> materialized)
        {
            rootActivity?.SetTag(ProcessFiles.FileCountTagName, materialized.Count);
            _logger?.LogFileCount(materialized.Count);
        }

        List<Exception>? exceptions = null;
        int fileCount = 0;
        foreach (FileInfo fileInfo in files)
        {
            fileCount++;
            using (Activity? processFileActivity = StartActivity(ProcessFile.ActivityName, ActivityKind.Internal, parent: rootActivity))
            {
                processFileActivity?.SetTag(ProcessFile.FilePathTagName, fileInfo.FullName);
                _logger?.ReadingFile(fileInfo.FullName, GetShortName(_reader));

                IngestionDocument? document = null;
                try
                {
                    document = await _reader.ReadAsync(fileInfo, cancellationToken).ConfigureAwait(false);

                    processFileActivity?.SetTag(ProcessSource.DocumentIdTagName, document.Identifier);
                    _logger?.ReadDocument(document.Identifier);

                    await IngestAsync(document, processFileActivity, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    TraceException(processFileActivity, ex);
                    _logger?.IngestingFailed(ex, document?.Identifier ?? fileInfo.FullName);

                    exceptions ??= [];
                    exceptions.Add(ex);

                    if (exceptions.Count >= _options.MaximumErrorsPerProcessing)
                    {
                        TraceException(rootActivity, ex);

                        if (exceptions.Count == 1)
                        {
                            throw;
                        }

                        throw new AggregateException(exceptions);
                    }

                    continue;
                }
            }
        }

        // When all ingestions throw, the method has to throw no matter how MaximumErrorsPerProcessing was configured.
        if (exceptions is not null && exceptions.Count == fileCount)
        {
            TraceException(rootActivity, exceptions[exceptions.Count - 1]);

            throw new AggregateException(exceptions);
        }
    }

    private async Task IngestAsync(IngestionDocument document, Activity? parentActivity, CancellationToken cancellationToken)
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
    }

    private Activity? StartActivity(string name, ActivityKind activityKind, Activity? parent = default)
    {
        if (!_activitySource.HasListeners())
        {
            return null;
        }
        else if (parent is null)
        {
            return _activitySource.StartActivity(name, activityKind);
        }

        return _activitySource.StartActivity(name, activityKind, parent.Context);
    }
}
