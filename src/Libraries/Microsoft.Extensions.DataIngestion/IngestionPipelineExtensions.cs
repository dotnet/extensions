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
/// Provides extension methods for the <see cref="IngestionPipeline{TChunk, TSource}"/> class.
/// </summary>
public static class IngestionPipelineExtensions
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

            foreach (var fileInfo in directory.EnumerateFiles(searchPattern, searchOption))
            {
                Exception? ex = null;
                try
                {
                    await pipeline.ProcessAsync(fileInfo, fileInfo.FullName, GetMediaType(fileInfo), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    ex = e;
                }

                yield return new IngestionResult(fileInfo.FullName, null, ex);
            }
        }
    }

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
