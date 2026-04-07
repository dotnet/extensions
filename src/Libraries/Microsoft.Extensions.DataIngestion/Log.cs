// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

#pragma warning disable S109 // Magic numbers should not be used

namespace Microsoft.Extensions.DataIngestion
{
    internal static partial class Log
    {
        [LoggerMessage(4, LogLevel.Information, "Writing chunks using {writer}.")]
        internal static partial void WritingChunks(this ILogger logger, string writer);

        [LoggerMessage(5, LogLevel.Information, "Wrote chunks for document '{documentId}'.")]
        internal static partial void WroteChunks(this ILogger logger, string documentId);

        [LoggerMessage(6, LogLevel.Error, "An error occurred while ingesting document '{identifier}'.")]
        internal static partial void IngestingFailed(this ILogger logger, Exception exception, string identifier);

        [LoggerMessage(7, LogLevel.Error, "The AI chat service returned {resultCount} instead of {expectedCount} results.")]
        internal static partial void UnexpectedResultsCount(this ILogger logger, int resultCount, int expectedCount);

        [LoggerMessage(8, LogLevel.Error, "Unexpected enricher failure.")]
        internal static partial void UnexpectedEnricherFailure(this ILogger logger, Exception exception);
    }
}
