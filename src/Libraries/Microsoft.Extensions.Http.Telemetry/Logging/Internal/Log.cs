// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Internal;

/// <summary>
/// Logs <see cref="HttpRequestMessage"/>, <see cref="HttpResponseMessage"/> and the exceptions due to errors of request/response.
/// </summary>
[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Workaround because Complex object logging does not support this.")]
[SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used", Justification = "Event ID's.")]
internal static partial class Log
{
    public static void OutgoingRequest(ILogger logger, LogLevel level, LogRecord record)
    {
        OutgoingRequest(logger, level, 1, nameof(OutgoingRequest), record);
    }

    public static void OutgoingRequestError(ILogger logger, LogRecord record, Exception exception)
    {
        OutgoingRequest(logger, LogLevel.Error, 2, nameof(OutgoingRequestError), record, exception);
    }

    [LogMethod(3, LogLevel.Error, "An error occurred while enriching the log record.")]
    public static partial void EnrichmentError(ILogger logger, Exception exception);

    // Using the code below to avoid every item in ILogger's logRecord State being prefixed with parameter name.
    // To be fixed in R9 later.
    private static void OutgoingRequest(
        ILogger logger, LogLevel level, int eventId, string eventName, LogRecord record, Exception? exception = null)
    {
        if (logger.IsEnabled(level))
        {
            var collector = record.EnrichmentProperties ?? LogMethodHelper.GetHelper();

            collector.AddRequestHeaders(record.RequestHeaders);
            collector.AddResponseHeaders(record.ResponseHeaders);
            collector.Add(HttpClientLoggingDimensions.Host, record.Host);
            collector.Add(HttpClientLoggingDimensions.Method, record.Method);
            collector.Add(HttpClientLoggingDimensions.Path, record.Path);
            collector.Add(HttpClientLoggingDimensions.Duration, record.Duration);

            if (record.StatusCode is not null)
            {
                collector.Add(HttpClientLoggingDimensions.StatusCode, record.StatusCode);
            }

            if (!string.IsNullOrEmpty(record.RequestBody))
            {
                collector.Add(HttpClientLoggingDimensions.RequestBody, record.RequestBody);
            }

            if (!string.IsNullOrEmpty(record.ResponseBody))
            {
                collector.Add(HttpClientLoggingDimensions.ResponseBody, record.ResponseBody);
            }

            logger.Log(
                level,
                new(eventId, eventName),
                collector,
                exception,
                static (_, _) => string.Empty);

            // Stryker disable once all
            if (collector != record.EnrichmentProperties)
            {
                LogMethodHelper.ReturnHelper(collector);
            }
        }
    }
}
