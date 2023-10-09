// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Http.Logging.Internal;

/// <summary>
/// Logs <see cref="HttpRequestMessage"/>, <see cref="HttpResponseMessage"/> and the exceptions due to errors of request/response.
/// </summary>
[SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used", Justification = "Event ID's.")]
internal static partial class Log
{
    internal const string OriginalFormat = "{OriginalFormat}";
    internal const string OriginalFormatValue =
        $"{HttpClientLoggingTagNames.Method} {HttpClientLoggingTagNames.Host}/{HttpClientLoggingTagNames.Path}";

    private const int MinimalPropertyCount = 4;

    private const string RequestReadErrorMessage =
        "An error occurred while reading the request data to fill the logger context for request: " +
        $"{{{HttpClientLoggingTagNames.Method}}} {{{HttpClientLoggingTagNames.Host}}}/{{{HttpClientLoggingTagNames.Path}}}";

    private const string ResponseReadErrorMessage =
        "An error occurred while reading the response data to fill the logger context for request: " +
        $"{{{HttpClientLoggingTagNames.Method}}} {{{HttpClientLoggingTagNames.Host}}}/{{{HttpClientLoggingTagNames.Path}}}";

    private const string LoggerContextMissingMessage =
        $"The logger couldn't read its context for {{RequestState}} request: {{{HttpClientLoggingTagNames.Method}}} {{{HttpClientLoggingTagNames.Host}}}";

    private const string EnrichmentErrorMessage =
        "An error occurred in enricher '{EnricherType}' while enriching the logger context for request: " +
        $"{{{HttpClientLoggingTagNames.Method}}} {{{HttpClientLoggingTagNames.Host}}}/{{{HttpClientLoggingTagNames.Path}}}";

    private static readonly Func<LoggerMessageState, Exception?, string> _originalFormatValueFMTFunc = OriginalFormatValueFMT;

    public static void OutgoingRequest(ILogger logger, LogLevel level, LogRecord record)
    {
        OutgoingRequest(logger, level, 1, nameof(OutgoingRequest), record);
    }

    public static void OutgoingRequestError(ILogger logger, LogRecord record, Exception exception)
    {
        OutgoingRequest(logger, LogLevel.Error, 2, nameof(OutgoingRequestError), record, exception);
    }

    [LoggerMessage(LogLevel.Error, RequestReadErrorMessage)]
    public static partial void RequestReadError(ILogger logger, Exception exception, HttpMethod httpMethod, string? httpHost, string? httpPath);

    [LoggerMessage(LogLevel.Error, ResponseReadErrorMessage)]
    public static partial void ResponseReadError(ILogger logger, Exception exception, HttpMethod httpMethod, string httpHost, string httpPath);

    [LoggerMessage(LogLevel.Error, LoggerContextMissingMessage)]
    public static partial void LoggerContextMissing(ILogger logger, Exception? exception, string requestState, HttpMethod httpMethod, string? httpHost);

    [LoggerMessage(LogLevel.Error, EnrichmentErrorMessage)]
    public static partial void EnrichmentError(ILogger logger, Exception exception, string? enricherType, HttpMethod httpMethod, string httpHost, string httpPath);

    // Using the code below instead of generated logging method because we have a custom formatter and custom tag keys for headers.
    private static void OutgoingRequest(
        ILogger logger, LogLevel level, int eventId, string eventName, LogRecord record, Exception? exception = null)
    {
        if (!logger.IsEnabled(level))
        {
            return;
        }

        // EnrichmentTags is null when we log request's start:
        var loggerMessageState = record.EnrichmentTags ?? LoggerMessageHelper.ThreadLocalState;

        var statusCodePropertyCount = record.StatusCode.HasValue ? 1 : 0;
        var requestHeadersCount = record.RequestHeaders?.Count ?? 0;
        var responseHeadersCount = record.ResponseHeaders?.Count ?? 0;

        var index = loggerMessageState.ReserveTagSpace(MinimalPropertyCount + statusCodePropertyCount + requestHeadersCount + responseHeadersCount);
        loggerMessageState.TagArray[index++] = new(HttpClientLoggingTagNames.Method, record.Method);
        loggerMessageState.TagArray[index++] = new(HttpClientLoggingTagNames.Host, record.Host);
        loggerMessageState.TagArray[index++] = new(HttpClientLoggingTagNames.Path, record.Path);
        loggerMessageState.TagArray[index++] = new(HttpClientLoggingTagNames.Duration, record.Duration);

        if (record.StatusCode.HasValue)
        {
            loggerMessageState.TagArray[index++] = new(HttpClientLoggingTagNames.StatusCode, record.StatusCode.Value);
        }

        if (requestHeadersCount > 0)
        {
            loggerMessageState.AddRequestHeaders(record.RequestHeaders!, ref index);
        }

        if (responseHeadersCount > 0)
        {
            loggerMessageState.AddResponseHeaders(record.ResponseHeaders!, ref index);
        }

        if (record.RequestBody is not null)
        {
            loggerMessageState.AddTag(HttpClientLoggingTagNames.RequestBody, record.RequestBody);
        }

        if (record.ResponseBody is not null)
        {
            loggerMessageState.AddTag(HttpClientLoggingTagNames.ResponseBody, record.ResponseBody);
        }

        logger.Log(
            level,
            new(eventId, eventName),
            loggerMessageState,
            exception,
            _originalFormatValueFMTFunc);

        if (record.EnrichmentTags is null)
        {
            loggerMessageState.Clear();
        }
    }

    private static string OriginalFormatValueFMT(LoggerMessageState request, Exception? _)
    {
        int startIndex = FindStartIndex(request);
        var httpMethod = request[startIndex].Value;
        var httpHost = request[startIndex + 1].Value;
        var httpPath = request[startIndex + 2].Value;
        return FormattableString.Invariant($"{httpMethod} {httpHost}/{httpPath}");
    }

    private static int FindStartIndex(LoggerMessageState request)
    {
        int startIndex = 0;

        foreach (var kvp in request)
        {
            if (kvp.Key == HttpClientLoggingTagNames.Method)
            {
                break;
            }

            startIndex++;
        }

        return startIndex;
    }
}
