// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Mime;
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
    /// Reads a stream and converts it to an <see cref="IngestionDocument"/>.
    /// </summary>
    /// <param name="source">The stream to read.</param>
    /// <param name="identifier">The unique identifier for the document.</param>
    /// <param name="mediaType">The media type of the content.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous read operation.</returns>
    public abstract Task<IngestionDocument> ReadAsync(Stream source, string identifier, string mediaType, CancellationToken cancellationToken = default);

    private static string GetMediaType(FileInfo source)
    {
        // Use MediaTypeMap for common media types
        var mediaType = MediaTypeMap.GetMediaType(source.Extension);
        if (mediaType is not null)
        {
            return mediaType;
        }

        // Fallback to specialized media types not in MediaTypeMap
        return source.Extension switch
        {
            ".123" => "application/vnd.lotus-1-2-3",
            ".602" => "application/x-t602",
            ".cgm" => "image/cgm",
            ".cwk" => "application/x-cwk",
            ".dif" => "application/x-dif",
            ".et" => "application/vnd.ms-excel",
            ".eth" => "application/ethos",
            ".fods" => "application/vnd.oasis.opendocument.spreadsheet",
            ".mcw" => "application/macwriteii",
            ".mw" => "application/macwriteii",
            ".pbd" => "application/x-pagemaker",
            ".prn" => "application/x-prn",
            ".qpw" => "application/x-quattro-pro",
            ".sdp" => "application/sdp",
            ".sgl" => "application/vnd.stardivision.writer",
            ".sylk" => "text/vnd.sylk",
            ".tiff" => "image/tiff",
            ".uof" => "application/vnd.uoml+xml",
            ".uop" => "application/vnd.openofficeorg.presentation",
            ".uos1" => "application/vnd.uoml+xml",
            ".uos2" => "application/vnd.uoml+xml",
            ".uot" => "application/x-uo",
            ".vor" => "application/vnd.stardivision.writer",
            ".wq1" => "application/x-lotus",
            ".wq2" => "application/x-lotus",
            ".xlr" => "application/vnd.ms-works",
            ".zabw" => "application/x-abiword",
            _ => "application/octet-stream"
        };
    }
}
