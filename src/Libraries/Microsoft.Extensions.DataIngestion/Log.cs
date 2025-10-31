// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

#pragma warning disable S109 // Magic numbers should not be used

namespace Microsoft.Extensions.DataIngestion
{
    internal static partial class Log
    {
        [LoggerMessage(0, LogLevel.Information, "Starting to process files in directory '{directory}' with search pattern '{searchPattern}' and search option '{searchOption}'.")]
        internal static partial void ProcessingDirectory(this ILogger logger, string directory, string searchPattern, System.IO.SearchOption searchOption);

        [LoggerMessage(1, LogLevel.Error, "An error occurred while processing files in directory '{directory}'.")]
        internal static partial void DirectoryError(this ILogger logger, Exception exception, string directory);

        [LoggerMessage(2, LogLevel.Error, "An error occurred while processing files.")]
        internal static partial void ProcessingError(this ILogger logger, Exception exception);

        [LoggerMessage(3, LogLevel.Information, "Processing {fileCount} files.")]
        internal static partial void LogFileCount(this ILogger logger, int fileCount);

        [LoggerMessage(4, LogLevel.Information, "Reading file '{filePath}' using '{reader}'.")]
        internal static partial void ReadingFile(this ILogger logger, string filePath, string reader);

        [LoggerMessage(5, LogLevel.Information, "Read document '{documentId}'.")]
        internal static partial void ReadDocument(this ILogger logger, string documentId);

        [LoggerMessage(6, LogLevel.Information, "Processing document '{documentId}' with '{processor}'.")]
        internal static partial void ProcessingDocument(this ILogger logger, string documentId, string processor);

        [LoggerMessage(7, LogLevel.Information, "Processed document '{documentId}'.")]
        internal static partial void ProcessedDocument(this ILogger logger, string documentId);
    }
}
