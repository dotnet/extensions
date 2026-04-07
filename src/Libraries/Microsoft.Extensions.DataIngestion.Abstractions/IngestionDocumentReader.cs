// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Reads source content and converts it to an <see cref="IngestionDocument"/>.
/// </summary>
public abstract class IngestionDocumentReader
{
    /// <summary>
    /// Reads a file and converts it to an <see cref="IngestionDocument"/>.
    /// </summary>
    /// <param name="source">The file to read.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous read operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public Task<IngestionDocument> ReadAsync(FileInfo source, CancellationToken cancellationToken = default)
    {
        string identifier = Throw.IfNull(source).FullName; // entire path is more unique than just part of it.
        return ReadAsync(source, identifier, GetMediaType(source), cancellationToken);
    }

    /// <summary>
    /// Reads a file and converts it to an <see cref="IngestionDocument"/>.
    /// </summary>
    /// <param name="source">The file to read.</param>
    /// <param name="identifier">The unique identifier for the document.</param>
    /// <param name="mediaType">The media type of the file.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous read operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="identifier"/> is <see langword="null"/> or empty.</exception>
    public virtual async Task<IngestionDocument> ReadAsync(FileInfo source, string identifier, string? mediaType = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(source);
        _ = Throw.IfNullOrEmpty(identifier);

        using FileStream stream = new(source.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1, FileOptions.Asynchronous);
        return await ReadAsync(stream, identifier, string.IsNullOrEmpty(mediaType) ? GetMediaType(source) : mediaType!, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads all files in the specified directory that match the given search pattern and option,
    /// and converts each to an <see cref="IngestionDocument"/>.
    /// </summary>
    /// <param name="directory">The directory to read.</param>
    /// <param name="searchPattern">The search pattern for file selection.</param>
    /// <param name="searchOption">The search option for directory traversal.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An asynchronous sequence of <see cref="IngestionDocument"/> instances.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="directory"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="searchPattern"/> is <see langword="null"/> or empty.</exception>
    public async IAsyncEnumerable<IngestionDocument> ReadAsync(
        DirectoryInfo directory,
        string searchPattern = "*.*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(directory);
        _ = Throw.IfNullOrEmpty(searchPattern);
        _ = Throw.IfOutOfRange((int)searchOption, (int)SearchOption.TopDirectoryOnly, (int)SearchOption.AllDirectories);

        await foreach (var document in ReadAsync(directory.EnumerateFiles(searchPattern, searchOption), cancellationToken).ConfigureAwait(false))
        {
            yield return document;
        }
    }

    /// <summary>
    /// Reads the specified files and converts each to an <see cref="IngestionDocument"/>.
    /// </summary>
    /// <param name="files">The files to read.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>An asynchronous sequence of <see cref="IngestionDocument"/> instances.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> is <see langword="null"/>.</exception>
    public async IAsyncEnumerable<IngestionDocument> ReadAsync(
        IEnumerable<FileInfo> files,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(files);

        foreach (FileInfo file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IngestionDocument document;
            try
            {
                document = await ReadAsync(file, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                document = new IngestionDocument(file.FullName) { ReadException = ex };
            }

            yield return document;
        }
    }

    /// <summary>
    /// Reads a stream and converts it to an <see cref="IngestionDocument"/>.
    /// </summary>
    /// <param name="source">The stream to read.</param>
    /// <param name="identifier">The unique identifier for the document.</param>
    /// <param name="mediaType">The media type of the content.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous read operation.</returns>
    public abstract Task<IngestionDocument> ReadAsync(Stream source, string identifier, string mediaType, CancellationToken cancellationToken = default);

    private static string GetMediaType(FileInfo source) =>
        MediaTypeMap.GetMediaType(source.Extension) ?? "application/octet-stream";
}
