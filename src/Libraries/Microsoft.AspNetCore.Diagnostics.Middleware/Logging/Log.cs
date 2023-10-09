// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Diagnostics.Logging;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

#pragma warning disable S109

internal static partial class Log
{
    internal const LogLevel DefaultLogLevel = LogLevel.Information;
    internal const LogLevel ErrorLogLevel = LogLevel.Error;
    internal const string OriginalFormat = "{OriginalFormat}";
    internal const string OriginalFormatValue =
        $"{HttpLoggingTagNames.Method} {HttpLoggingTagNames.Host}/{HttpLoggingTagNames.Path}";
    internal const string ReadingRequestBodyError = "Error on reading HTTP request body.";
    internal const string ReadingResponseBodyError = "Error on reading HTTP response body.";

    private const int IncomingRequestEventId = 1;
    private const int RequestProcessingErrorEventId = 2;

#pragma warning disable S3257 // Declarations and initializations should be as concise as possible
    private static readonly Func<IncomingRequestStruct, Exception?, string> _originalFormatValueFMTFunc = new(OriginalFormatValueFMT);
#pragma warning restore S3257 // Declarations and initializations should be as concise as possible

    #region Non-generated logging

    public static void IncomingRequest(this ILogger logger, IncomingRequestLogRecord req)
    {
        if (logger.IsEnabled(DefaultLogLevel))
        {
            var collector = req.EnrichmentPropertyBag ?? LogMethodHelper.GetHelper();

            try
            {
                collector.ParameterName = string.Empty;
                HttpLogPropertiesProvider.GetTags(collector, req);

                collector.Add(OriginalFormat, OriginalFormatValue);
                logger.Log(
                    DefaultLogLevel,
                    new(IncomingRequestEventId, nameof(IncomingRequest)),
                    new IncomingRequestStruct(collector),
                    null,
                    _originalFormatValueFMTFunc);
            }
            finally
            {
                // Stryker disable once all
                if (collector != req.EnrichmentPropertyBag)
                {
                    LogMethodHelper.ReturnHelper(collector);
                }
            }
        }
    }

    public static void RequestProcessingError(this ILogger logger, Exception ex, IncomingRequestLogRecord req)
    {
        if (logger.IsEnabled(ErrorLogLevel))
        {
            var collector = req.EnrichmentPropertyBag ?? LogMethodHelper.GetHelper();

            try
            {
                collector.ParameterName = string.Empty;
                HttpLogPropertiesProvider.GetTags(collector, req);

                collector.Add(OriginalFormat, OriginalFormatValue);
                logger.Log(
                    ErrorLogLevel,
                    new(RequestProcessingErrorEventId, nameof(RequestProcessingError)),
                    new IncomingRequestStruct(collector),
                    ex,
                    _originalFormatValueFMTFunc);
            }
            finally
            {
                // Stryker disable once all
                if (collector != req.EnrichmentPropertyBag)
                {
                    LogMethodHelper.ReturnHelper(collector);
                }
            }
        }
    }

    internal readonly struct IncomingRequestStruct : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private readonly LogMethodHelper _collector;

        public IncomingRequestStruct(LogMethodHelper collector)
        {
            _collector = collector;
        }

        public int Count
            => _collector.Count;

        public KeyValuePair<string, object?> this[int index]
            => _collector[index];

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    #endregion

    [LoggerMessage(3, LogLevel.Error, ReadingRequestBodyError)]
    public static partial void ErrorReadingRequestBody(this ILogger logger, Exception ex);

    [LoggerMessage(4, LogLevel.Error, ReadingResponseBodyError)]
    public static partial void ErrorReadingResponseBody(this ILogger logger, Exception ex);

#pragma warning disable LOGGEN000
    [LoggerMessage(5, LogLevel.Warning,
        $"HttpLogging middleware is injected into application pipeline, but {nameof(LogLevel)} '{{LogLevel}}' is disabled in logger. " +
        "Remove {MethodName}() call from pipeline configuration in that case.")]
    public static partial void MiddlewareIsMisused(this ILogger logger, LogLevel logLevel, string methodName);
#pragma warning restore LOGGEN000

    private static string OriginalFormatValueFMT(IncomingRequestStruct request, Exception? _)
    {
        int startIndex = FindStartIndex(request);
        var httpMethod = request[startIndex].Value;
        var httpHost = request[startIndex + 1].Value;
        var httpPath = request[startIndex + 2].Value;
        return FormattableString.Invariant($"{httpMethod} {httpHost}/{httpPath}");
    }

    [ExcludeFromCodeCoverage]
    private static int FindStartIndex(in IncomingRequestStruct request)
    {
        int startIndex = 0;

        foreach (var kvp in request)
        {
            if (kvp.Key == HttpLoggingTagNames.Method)
            {
                break;
            }

            startIndex++;
        }

        return startIndex;
    }
}

