﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
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
        => source.Extension switch
        {
            ".123" => "application/vnd.lotus-1-2-3",
            ".602" => "application/x-t602",
            ".abw" => "application/x-abiword",
            ".bmp" => "image/bmp",
            ".cgm" => "image/cgm",
            ".csv" => "text/csv",
            ".cwk" => "application/x-cwk",
            ".dbf" => "application/vnd.dbf",
            ".dif" => "application/x-dif",
            ".doc" => "application/msword",
            ".docm" => "application/vnd.ms-word.document.macroEnabled.12",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".dot" => "application/msword",
            ".dotm" => "application/vnd.ms-word.template.macroEnabled.12",
            ".dotx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.template",
            ".epub" => "application/epub+zip",
            ".et" => "application/vnd.ms-excel",
            ".eth" => "application/ethos",
            ".fods" => "application/vnd.oasis.opendocument.spreadsheet",
            ".gif" => "image/gif",
            ".htm" => "text/html",
            ".html" => "text/html",
            ".hwp" => "application/x-hwp",
            ".jpeg" => "image/jpeg",
            ".jpg" => "image/jpeg",
            ".key" => "application/x-iwork-keynote-sffkey",
            ".lwp" => "application/vnd.lotus-wordpro",
            ".mcw" => "application/macwriteii",
            ".mw" => "application/macwriteii",
            ".numbers" => "application/x-iwork-numbers-sffnumbers",
            ".ods" => "application/vnd.oasis.opendocument.spreadsheet",
            ".pages" => "application/x-iwork-pages-sffpages",
            ".pbd" => "application/x-pagemaker",
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".pot" => "application/vnd.ms-powerpoint",
            ".potm" => "application/vnd.ms-powerpoint.template.macroEnabled.12",
            ".potx" => "application/vnd.openxmlformats-officedocument.presentationml.template",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptm" => "application/vnd.ms-powerpoint.presentation.macroEnabled.12",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".prn" => "application/x-prn",
            ".qpw" => "application/x-quattro-pro",
            ".rtf" => "application/rtf",
            ".sda" => "application/vnd.stardivision.draw",
            ".sdd" => "application/vnd.stardivision.impress",
            ".sdp" => "application/sdp",
            ".sdw" => "application/vnd.stardivision.writer",
            ".sgl" => "application/vnd.stardivision.writer",
            ".slk" => "text/vnd.sylk",
            ".sti" => "application/vnd.sun.xml.impress.template",
            ".stw" => "application/vnd.sun.xml.writer.template",
            ".svg" => "image/svg+xml",
            ".sxg" => "application/vnd.sun.xml.writer.global",
            ".sxi" => "application/vnd.sun.xml.impress",
            ".sxw" => "application/vnd.sun.xml.writer",
            ".sylk" => "text/vnd.sylk",
            ".tiff" => "image/tiff",
            ".tsv" => "text/tab-separated-values",
            ".txt" => "text/plain",
            ".uof" => "application/vnd.uoml+xml",
            ".uop" => "application/vnd.openofficeorg.presentation",
            ".uos1" => "application/vnd.uoml+xml",
            ".uos2" => "application/vnd.uoml+xml",
            ".uot" => "application/x-uo",
            ".vor" => "application/vnd.stardivision.writer",
            ".webp" => "image/webp",
            ".wpd" => "application/wordperfect",
            ".wps" => "application/vnd.ms-works",
            ".wq1" => "application/x-lotus",
            ".wq2" => "application/x-lotus",
            ".xls" => "application/vnd.ms-excel",
            ".xlsb" => "application/vnd.ms-excel.sheet.binary.macroEnabled.12",
            ".xlsm" => "application/vnd.ms-excel.sheet.macroEnabled.12",
            ".xlr" => "application/vnd.ms-works",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xlw" => "application/vnd.ms-excel",
            ".xml" => "application/xml",
            ".zabw" => "application/x-abiword",
            _ => "application/octet-stream"
        };
}
