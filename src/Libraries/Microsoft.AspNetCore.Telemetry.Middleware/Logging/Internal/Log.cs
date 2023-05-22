// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging;

#pragma warning disable S109

internal static partial class Log
{
    internal const LogLevel DefaultLogLevel = LogLevel.Information;
    internal const LogLevel ErrorLogLevel = LogLevel.Error;
    internal const string OriginalFormatValue = "";
    internal const string ReadingRequestBodyError = "Error on reading HTTP request body.";
    internal const string ReadingResponseBodyError = "Error on reading HTTP response body.";

    private const int IncomingRequestEventId = 1;
    private const int RequestProcessingErrorEventId = 2;

    #region Non-generated logging

    public static void IncomingRequest(this ILogger logger, IncomingRequestLogRecord req)
    {
        if (logger.IsEnabled(DefaultLogLevel))
        {
            var collector = req.EnrichmentPropertyBag ?? LogMethodHelper.GetHelper();

            try
            {
                collector.ParameterName = string.Empty;
                HttpLogPropertiesProvider.GetProperties(collector, req);

                logger.Log(
                    DefaultLogLevel,
                    new(IncomingRequestEventId, nameof(IncomingRequest)),
                    new IncomingRequestStruct(collector),
                    null,
                    static (_, _) => OriginalFormatValue);
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
                HttpLogPropertiesProvider.GetProperties(collector, req);

                logger.Log(
                    ErrorLogLevel,
                    new(RequestProcessingErrorEventId, nameof(RequestProcessingError)),
                    new IncomingRequestStruct(collector),
                    ex,
                    static (_, _) => OriginalFormatValue);
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

    [LogMethod(3, LogLevel.Error, ReadingRequestBodyError)]
    public static partial void ErrorReadingRequestBody(this ILogger logger, Exception ex);

    [LogMethod(4, LogLevel.Error, ReadingResponseBodyError)]
    public static partial void ErrorReadingResponseBody(this ILogger logger, Exception ex);

#pragma warning disable R9G001
    [LogMethod(5, LogLevel.Warning,
        $"HttpLogging middleware is injected into application pipeline, but {nameof(LogLevel)} '{{logLevel}}' is disabled in logger. " +
        "Remove {methodName}() call from pipeline configuration in that case.")]
    public static partial void MiddlewareIsMisused(this ILogger logger, LogLevel logLevel, string methodName);
#pragma warning restore R9G001
}

