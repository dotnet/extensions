// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using static Microsoft.Extensions.DataIngestion.DiagnosticsConstants;

namespace Microsoft.Extensions.DataIngestion;

#pragma warning disable IDE0058 // Expression value is never used
#pragma warning disable IDE0063 // Use simple 'using' statement
#pragma warning disable CA1031 // Do not catch general exception types

/// <summary>
/// Provides a set of File System extension methods for the <see cref="IngestionPipeline{TChunk, TSource}"/> class.
/// </summary>
public static class FileSystemIngestionExtensions
{
    /// <summary>
    /// Processes all files in the specified directory that match the given search pattern and option.
    /// </summary>
    /// <typeparam name="TChunk">The type of the chunk content.</typeparam>
    /// <param name="pipeline">The ingestion pipeline.</param>
    /// <param name="directory">The directory to process.</param>
    /// <param name="searchPattern">The search pattern for file selection.</param>
    /// <param name="searchOption">The search option for directory traversal.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async IAsyncEnumerable<IngestionResult> ProcessAsync<TChunk>(
        this IngestionPipeline<TChunk, FileInfo> pipeline,
        DirectoryInfo directory, string searchPattern = "*.*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Throw.IfNull(pipeline);
        Throw.IfNull(directory);
        Throw.IfNullOrEmpty(searchPattern);
        Throw.IfOutOfRange((int)searchOption, (int)SearchOption.TopDirectoryOnly, (int)SearchOption.AllDirectories);

        using (Activity? rootActivity = pipeline.ActivitySource.StartActivity(ProcessDirectory.ActivityName))
        {
            rootActivity?.SetTag(ProcessDirectory.DirectoryPathTagName, directory.FullName)
                         .SetTag(ProcessDirectory.SearchPatternTagName, searchPattern)
                         .SetTag(ProcessDirectory.SearchOptionTagName, searchOption.ToString());
            pipeline.Logger?.ProcessingDirectory(directory.FullName, searchPattern, searchOption);

            var files = directory.GetFiles(searchPattern, searchOption);
            await foreach (var ingestionResult in pipeline.ProcessAsync(files, rootActivity, cancellationToken).ConfigureAwait(false))
            {
                yield return ingestionResult;
            }
        }
    }

    /// <summary>
    /// Processes the specified files.
    /// </summary>
    /// <typeparam name="TChunk">The type of the chunk content.</typeparam>
    /// <param name="pipeline">The ingestion pipeline.</param>
    /// <param name="files">The collection of files to process.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async IAsyncEnumerable<IngestionResult> ProcessAsync<TChunk>(
        this IngestionPipeline<TChunk, FileInfo> pipeline,
        IEnumerable<FileInfo> files,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Throw.IfNull(pipeline);
        Throw.IfNull(files);

        using (Activity? rootActivity = pipeline.ActivitySource.StartActivity(ProcessFiles.ActivityName))
        {
            await foreach (var ingestionResult in pipeline.ProcessAsync(files, rootActivity, cancellationToken).ConfigureAwait(false))
            {
                yield return ingestionResult;
            }
        }
    }

    private static async IAsyncEnumerable<IngestionResult> ProcessAsync<TChunk>(
        this IngestionPipeline<TChunk, FileInfo> pipeline,
        IEnumerable<FileInfo> files, Activity? rootActivity,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
#if NET
        if (System.Linq.Enumerable.TryGetNonEnumeratedCount(files, out int count))
#else
        if (files is IReadOnlyCollection<FileInfo> { Count: int count })
#endif
        {
            rootActivity?.SetTag(ProcessFiles.FileCountTagName, count);
            pipeline.Logger?.LogFileCount(count);
        }

        foreach (FileInfo fileInfo in files)
        {
            using (Activity? processFileActivity = pipeline.ActivitySource.StartActivity(ProcessFile.ActivityName, ActivityKind.Internal, parentContext: rootActivity?.Context ?? default))
            {
                processFileActivity?.SetTag(ProcessFile.FilePathTagName, fileInfo.FullName);

                Exception? failure = null;
                IngestionDocument? document = null;

                try
                {
                    document = await pipeline.ProcessAsync(fileInfo, fileInfo.FullName, fileInfo.GetMediaType(), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    failure = e;
                }

                string documentId = document?.Identifier ?? fileInfo.FullName;
                yield return new IngestionResult(documentId, document, failure);
            }
        }
    }
}
