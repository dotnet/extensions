// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Extensions.Logging;

#pragma warning disable S109 // Magic numbers should not be used

namespace Microsoft.Extensions.DataIngestion
{
    internal static partial class Log
    {
        [LoggerMessage(0, LogLevel.Information, "Starting to process files in directory '{directory}' with search pattern '{searchPattern}' and search option '{searchOption}'.")]
        internal static partial void ProcessingDirectory(this ILogger logger, string directory, string searchPattern, System.IO.SearchOption searchOption);

        [LoggerMessage(1, LogLevel.Information, "Processing {fileCount} files.")]
        internal static partial void LogFileCount(this ILogger logger, int fileCount);

        [LoggerMessage(2, LogLevel.Information, "Reading file '{filePath}' using '{reader}'.")]
        internal static partial void ReadingFile(this ILogger logger, string filePath, string reader);

        [LoggerMessage(3, LogLevel.Information, "Read document '{documentId}'.")]
        internal static partial void ReadDocument(this ILogger logger, string documentId);

        [LoggerMessage(4, LogLevel.Information, "Writing chunks using {writer}.")]
        internal static partial void WritingChunks(this ILogger logger, string writer);

        [LoggerMessage(5, LogLevel.Information, "Wrote chunks for document '{documentId}'.")]
        internal static partial void WroteChunks(this ILogger logger, string documentId);

        [LoggerMessage(6, LogLevel.Error, "An error occurred while ingesting document '{identifier}'.")]
        internal static partial void IngestingFailed(this ILogger logger, Exception exception, string identifier);
    }
}
