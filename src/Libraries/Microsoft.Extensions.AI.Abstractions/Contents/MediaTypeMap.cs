// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

#pragma warning disable CA1307 // Specify StringComparison for clarity

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides methods for mapping between file extensions and media types (MIME types).
/// </summary>
/// <remarks>
/// This is a polyfill for the MediaTypeMap class in System.Net.Mime.
/// For more info, see https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Mail/src/System/Net/Mime/MediaTypeMap.cs.
/// </remarks>
internal static class MediaTypeMap
{
    /// <summary>The default media type for unknown file extensions.</summary>
    internal const string DefaultMediaType = "application/octet-stream";

    /// <summary>Maps file extensions to media types.</summary>
    private static readonly Dictionary<string, string> _extensionToMediaType = new(StringComparer.OrdinalIgnoreCase)
    {
        [".3g2"] = "video/3gpp2",
        [".3gp"] = "video/3gpp",
        [".3gp2"] = "video/3gpp2",
        [".3gpp"] = "video/3gpp",
        [".7z"] = "application/x-7z-compressed",
        [".aac"] = "audio/aac",
        [".ai"] = "application/postscript",
        [".aif"] = "audio/x-aiff",
        [".aifc"] = "audio/aifc",
        [".aiff"] = "audio/aiff",
        [".apng"] = "image/apng",
        [".avi"] = "video/x-msvideo",
        [".avif"] = "image/avif",
        [".bmp"] = "image/bmp",
        [".bz"] = "application/x-bzip",
        [".bz2"] = "application/x-bzip2",
        [".css"] = "text/css",
        [".csv"] = "text/csv",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".epub"] = "application/epub+zip",
        [".flac"] = "audio/flac",
        [".gif"] = "image/gif",
        [".gz"] = "application/gzip",
        [".heic"] = "image/heic",
        [".heif"] = "image/heif",
        [".htm"] = "text/html",
        [".html"] = "text/html",
        [".ico"] = "image/vnd.microsoft.icon",
        [".jar"] = "application/java-archive",
        [".jfif"] = "image/jpeg",
        [".jpe"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".jpg"] = "image/jpeg",
        [".js"] = "text/javascript",
        [".json"] = "application/json",
        [".jxl"] = "image/jxl",
        [".m4a"] = "audio/mp4",
        [".m4v"] = "video/mp4",
        [".md"] = "text/markdown",
        [".mid"] = "audio/midi",
        [".midi"] = "audio/midi",
        [".mjs"] = "text/javascript",
        [".mkv"] = "video/x-matroska",
        [".mov"] = "video/quicktime",
        [".mp3"] = "audio/mpeg",
        [".mp4"] = "video/mp4",
        [".mpeg"] = "video/mpeg",
        [".mpg"] = "video/mpeg",
        [".oga"] = "audio/ogg",
        [".ogg"] = "audio/ogg",
        [".ogv"] = "video/ogg",
        [".opus"] = "audio/opus",
        [".otf"] = "font/otf",
        [".pdf"] = "application/pdf",
        [".png"] = "image/png",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".rar"] = "application/vnd.rar",
        [".rtf"] = "application/rtf",
        [".svg"] = "image/svg+xml",
        [".svgz"] = "image/svg+xml",
        [".tar"] = "application/x-tar",
        [".tif"] = "image/tiff",
        [".tiff"] = "image/tiff",
        [".ts"] = "text/typescript",
        [".ttf"] = "font/ttf",
        [".txt"] = "text/plain",
        [".wasm"] = "application/wasm",
        [".wav"] = "audio/wav",
        [".weba"] = "audio/webm",
        [".webm"] = "video/webm",
        [".webp"] = "image/webp",
        [".woff"] = "font/woff",
        [".woff2"] = "font/woff2",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".xml"] = "application/xml",
        [".yaml"] = "application/yaml",
        [".yml"] = "application/yaml",
        [".zip"] = "application/zip",
    };

    /// <summary>Maps media types to file extensions.</summary>
    private static readonly Dictionary<string, string> _mediaTypeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["application/epub+zip"] = ".epub",
        ["application/gzip"] = ".gz",
        ["application/java-archive"] = ".jar",
        ["application/json"] = ".json",
        ["application/msword"] = ".doc",
        ["application/octet-stream"] = ".bin",
        ["application/pdf"] = ".pdf",
        ["application/postscript"] = ".ps",
        ["application/rtf"] = ".rtf",
        ["application/vnd.ms-excel"] = ".xls",
        ["application/vnd.ms-powerpoint"] = ".ppt",
        ["application/vnd.openxmlformats-officedocument.presentationml.presentation"] = ".pptx",
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = ".xlsx",
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = ".docx",
        ["application/vnd.rar"] = ".rar",
        ["application/wasm"] = ".wasm",
        ["application/x-7z-compressed"] = ".7z",
        ["application/x-bzip"] = ".bz",
        ["application/x-bzip2"] = ".bz2",
        ["application/x-tar"] = ".tar",
        ["application/xml"] = ".xml",
        ["application/yaml"] = ".yaml",
        ["application/zip"] = ".zip",
        ["audio/aac"] = ".aac",
        ["audio/aifc"] = ".aifc",
        ["audio/aiff"] = ".aiff",
        ["audio/flac"] = ".flac",
        ["audio/midi"] = ".mid",
        ["audio/mp4"] = ".m4a",
        ["audio/mpeg"] = ".mp3",
        ["audio/ogg"] = ".oga",
        ["audio/opus"] = ".opus",
        ["audio/wav"] = ".wav",
        ["audio/webm"] = ".weba",
        ["audio/x-aiff"] = ".aif",
        ["font/otf"] = ".otf",
        ["font/ttf"] = ".ttf",
        ["font/woff"] = ".woff",
        ["font/woff2"] = ".woff2",
        ["image/apng"] = ".apng",
        ["image/avif"] = ".avif",
        ["image/bmp"] = ".bmp",
        ["image/gif"] = ".gif",
        ["image/heic"] = ".heic",
        ["image/heif"] = ".heif",
        ["image/jpeg"] = ".jpg",
        ["image/jxl"] = ".jxl",
        ["image/png"] = ".png",
        ["image/svg+xml"] = ".svg",
        ["image/tiff"] = ".tif",
        ["image/vnd.microsoft.icon"] = ".ico",
        ["image/webp"] = ".webp",
        ["text/css"] = ".css",
        ["text/csv"] = ".csv",
        ["text/html"] = ".html",
        ["text/javascript"] = ".js",
        ["text/markdown"] = ".md",
        ["text/plain"] = ".txt",
        ["text/typescript"] = ".ts",
        ["video/3gpp"] = ".3gp",
        ["video/3gpp2"] = ".3g2",
        ["video/mp4"] = ".mp4",
        ["video/mpeg"] = ".mpeg",
        ["video/ogg"] = ".ogv",
        ["video/quicktime"] = ".mov",
        ["video/webm"] = ".webm",
        ["video/x-matroska"] = ".mkv",
        ["video/x-msvideo"] = ".avi",
    };

    /// <summary>
    /// Gets the media type (MIME type) for the specified file path or extension.
    /// </summary>
    /// <param name="pathOrExtension">A file path or extension (with or without leading period).</param>
    /// <returns>The media type associated with the extension, or <see langword="null"/> if no mapping exists.</returns>
    public static string? GetMediaType(string pathOrExtension)
    {
        if (string.IsNullOrEmpty(pathOrExtension))
        {
            return null;
        }

        string extension = Path.GetExtension(pathOrExtension);

        if (string.IsNullOrEmpty(extension))
        {
            // The input might be an extension itself (e.g., ".pdf" or "pdf")
            extension = pathOrExtension;
            if (extension[0] != '.')
            {
                extension = "." + extension;
            }
        }

        _ = _extensionToMediaType.TryGetValue(extension, out string? result);
        return result;
    }

    /// <summary>
    /// Gets the file extension for the specified media type (MIME type).
    /// </summary>
    /// <param name="mediaType">The media type (e.g. "application/pdf").</param>
    /// <returns>The file extension (with leading period) associated with the media type, or <see langword="null"/> if no mapping exists.</returns>
    public static string? GetExtension(string mediaType)
    {
        if (string.IsNullOrEmpty(mediaType))
        {
            return null;
        }

        // Remove any parameters from the media type (e.g., "text/html; charset=utf-8")
        int semicolonIndex = mediaType.IndexOf(';');
        if (semicolonIndex >= 0)
        {
            mediaType = mediaType.Substring(0, semicolonIndex).Trim();
        }

        _ = _mediaTypeToExtension.TryGetValue(mediaType, out string? value);
        return value;
    }
}
