// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DataIngestion;

// Design notes: this class no longer exposes an overload that takes a Stream and a CancellationToken.
// The reason is that Stream does not provide the necessary information like the MIME type or the file name.
public abstract class IngestionDocumentReader
{
    public Task<IngestionDocument> ReadAsync(FileInfo source, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IngestionDocument>(cancellationToken);
        }

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        string identifier = source.FullName; // entire path is more unique than just part of it.
        return ReadAsync(source, identifier, GetMediaType(source), cancellationToken);
    }

    public virtual Task<IngestionDocument> ReadAsync(FileInfo source, string identifier, string? mediaType = null, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<IngestionDocument>(cancellationToken);
        }

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        else if (string.IsNullOrEmpty(identifier))
        {
            throw new ArgumentNullException(nameof(identifier));
        }

        using FileStream stream = new(source.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1, FileOptions.Asynchronous);
        return ReadAsync(stream, identifier, string.IsNullOrEmpty(mediaType) ? GetMediaType(source) : mediaType!, cancellationToken);
    }

    public abstract Task<IngestionDocument> ReadAsync(Stream source, string identifier, string mediaType, CancellationToken cancellationToken = default);

    private string GetMediaType(FileInfo source)
        => source.Extension switch
        {
            ".pdf" => "application/pdf",
            ".602" => "application/x-t602",
            ".abw" => "application/x-abiword",
            ".cgm" => "image/cgm",
            ".cwk" => "application/x-cwk",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".docm" => "application/vnd.ms-word.document.macroEnabled.12",
            ".dot" => "application/msword",
            ".dotm" => "application/vnd.ms-word.template.macroEnabled.12",
            ".dotx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.template",
            ".hwp" => "application/x-hwp",
            ".key" => "application/x-iwork-keynote-sffkey",
            ".lwp" => "application/vnd.lotus-wordpro",
            ".mw" => "application/macwriteii",
            ".mcw" => "application/macwriteii",
            ".pages" => "application/x-iwork-pages-sffpages",
            ".pbd" => "application/x-pagemaker",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptm" => "application/vnd.ms-powerpoint.presentation.macroEnabled.12",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".pot" => "application/vnd.ms-powerpoint",
            ".potm" => "application/vnd.ms-powerpoint.template.macroEnabled.12",
            ".potx" => "application/vnd.openxmlformats-officedocument.presentationml.template",
            ".rtf" => "application/rtf",
            ".sda" => "application/vnd.stardivision.draw",
            ".sdd" => "application/vnd.stardivision.impress",
            ".sdp" => "application/sdp",
            ".sdw" => "application/vnd.stardivision.writer",
            ".sgl" => "application/vnd.stardivision.writer",
            ".sti" => "application/vnd.sun.xml.impress.template",
            ".sxi" => "application/vnd.sun.xml.impress",
            ".sxw" => "application/vnd.sun.xml.writer",
            ".stw" => "application/vnd.sun.xml.writer.template",
            ".sxg" => "application/vnd.sun.xml.writer.global",
            ".txt" => "text/plain",
            ".uof" => "application/vnd.uoml+xml",
            ".uop" => "application/vnd.openofficeorg.presentation",
            ".uot" => "application/x-uo",
            ".vor" => "application/vnd.stardivision.writer",
            ".wpd" => "application/wordperfect",
            ".wps" => "application/vnd.ms-works",
            ".xml" => "application/xml",
            ".zabw" => "application/x-abiword",
            ".epub" => "application/epub+zip",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            ".tiff" => "image/tiff",
            ".webp" => "image/webp",
            ".htm" => "text/html",
            ".html" => "text/html",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".xlsm" => "application/vnd.ms-excel.sheet.macroEnabled.12",
            ".xlsb" => "application/vnd.ms-excel.sheet.binary.macroEnabled.12",
            ".xlw" => "application/vnd.ms-excel",
            ".csv" => "text/csv",
            ".dif" => "application/x-dif",
            ".sylk" => "text/vnd.sylk",
            ".slk" => "text/vnd.sylk",
            ".prn" => "application/x-prn",
            ".numbers" => "application/x-iwork-numbers-sffnumbers",
            ".et" => "application/vnd.ms-excel",
            ".ods" => "application/vnd.oasis.opendocument.spreadsheet",
            ".fods" => "application/vnd.oasis.opendocument.spreadsheet",
            ".uos1" => "application/vnd.uoml+xml",
            ".uos2" => "application/vnd.uoml+xml",
            ".dbf" => "application/vnd.dbf",
            ".wk1" => "application/vnd.lotus-1-2-3",
            ".wk2" => "application/vnd.lotus-1-2-3",
            ".wk3" => "application/vnd.lotus-1-2-3",
            ".wk4" => "application/vnd.lotus-1-2-3",
            ".wks" => "application/vnd.lotus-1-2-3",
            ".123" => "application/vnd.lotus-1-2-3",
            ".wq1" => "application/x-lotus",
            ".wq2" => "application/x-lotus",
            ".wb1" => "application/x-quattro-pro",
            ".wb2" => "application/x-quattro-pro",
            ".wb3" => "application/x-quattro-pro",
            ".qpw" => "application/x-quattro-pro",
            ".xlr" => "application/vnd.ms-works",
            ".eth" => "application/ethos",
            ".tsv" => "text/tab-separated-values",
            _ => "", // only some readers require the media type, so we return empty string here and they are expected to handle it.
        };
}
