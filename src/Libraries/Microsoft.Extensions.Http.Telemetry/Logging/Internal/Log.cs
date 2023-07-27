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
    internal const string OriginalFormat = "{OriginalFormat}";
    internal const string OriginalFormatValue = "{httpMethod} {httpHost}/{httpPath}";

    private const string RequestReadErrorMessage =
        "An error occurred while reading the request data to fill the log record: " +
        $"{{{HttpClientLoggingDimensions.Method}}} {{{HttpClientLoggingDimensions.Host}}}/{{{HttpClientLoggingDimensions.Path}}}";

    private const string ResponseReadErrorMessage =
        "An error occurred while reading the response data to fill the log record: " +
        $"{{{HttpClientLoggingDimensions.Method}}} {{{HttpClientLoggingDimensions.Host}}}/{{{HttpClientLoggingDimensions.Path}}}";

    private const string EnrichmentErrorMessage =
        "An error occurred while enriching the log record: " +
        $"{{{HttpClientLoggingDimensions.Method}}} {{{HttpClientLoggingDimensions.Host}}}/{{{HttpClientLoggingDimensions.Path}}}";

    private static readonly Func<LogMethodHelper, Exception?, string> _originalFormatValueFMTFunc = OriginalFormatValueFMT;

    public static void OutgoingRequest(ILogger logger, LogLevel level, LogRecord record)
    {
        OutgoingRequest(logger, level, 1, nameof(OutgoingRequest), record);
    }

    public static void OutgoingRequestError(ILogger logger, LogRecord record, Exception exception)
    {
        OutgoingRequest(logger, LogLevel.Error, 2, nameof(OutgoingRequestError), record, exception);
    }

    [LogMethod(LogLevel.Error, RequestReadErrorMessage)]
    public static partial void RequestReadError(ILogger logger, Exception exception, HttpMethod httpMethod, string? httpHost, string httpPath);

    [LogMethod(LogLevel.Error, ResponseReadErrorMessage)]
    public static partial void ResponseReadError(ILogger logger, Exception exception, HttpMethod httpMethod, string httpHost, string httpPath);

    [LogMethod(LogLevel.Error, EnrichmentErrorMessage)]
    public static partial void EnrichmentError(ILogger logger, Exception exception, HttpMethod httpMethod, string httpHost, string httpPath);

    // Using the code below instead of generated logging method because we have a custom formatter and custom tag keys for headers.
    private static void OutgoingRequest(
        ILogger logger, LogLevel level, int eventId, string eventName, LogRecord record, Exception? exception = null)
    {
        if (logger.IsEnabled(level))
        {
            var collector = record.EnrichmentProperties ?? LogMethodHelper.GetHelper();

            collector.AddRequestHeaders(record.RequestHeaders);
            collector.AddResponseHeaders(record.ResponseHeaders);
            collector.Add(HttpClientLoggingDimensions.Method, record.Method);
            collector.Add(HttpClientLoggingDimensions.Host, record.Host);
            collector.Add(HttpClientLoggingDimensions.Path, record.Path);
            collector.Add(HttpClientLoggingDimensions.Duration, record.Duration);

            if (record.StatusCode.HasValue)
            {
                collector.Add(HttpClientLoggingDimensions.StatusCode, record.StatusCode.Value);
            }

            if (record.RequestBody is not null)
            {
                collector.Add(HttpClientLoggingDimensions.RequestBody, record.RequestBody);
            }

            if (record.ResponseBody is not null)
            {
                collector.Add(HttpClientLoggingDimensions.ResponseBody, record.ResponseBody);
            }

            logger.Log(
                level,
                new(eventId, eventName),
                collector,
                exception,
                _originalFormatValueFMTFunc);

            // Stryker disable once all
            if (collector != record.EnrichmentProperties)
            {
                LogMethodHelper.ReturnHelper(collector);
            }
        }
    }

    private static string OriginalFormatValueFMT(LogMethodHelper request, Exception? _)
    {
        int startIndex = FindStartIndex(request);
        var httpMethod = request[startIndex].Value;
        var httpHost = request[startIndex + 1].Value;
        var httpPath = request[startIndex + 2].Value;
        return FormattableString.Invariant($"{httpMethod} {httpHost}/{httpPath}");
    }

    private static int FindStartIndex(LogMethodHelper request)
    {
        int startIndex = 0;

        foreach (var kvp in request)
        {
            if (kvp.Key == HttpClientLoggingDimensions.Method)
            {
                break;
            }

            startIndex++;
        }

        return startIndex;
    }
}
