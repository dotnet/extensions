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
internal static class Log
{
    internal const string OriginalFormat = "{OriginalFormat}";
    private const string NullString = "(null)";

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
        "An error occurred in enricher '{Enricher}' while enriching the logger context for request: " +
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

    public static void RequestReadError(ILogger logger, Exception exception, HttpMethod method, string? host, string? path)
    {
        var state = LoggerMessageHelper.ThreadLocalState;

        _ = state.ReserveTagSpace(4);
        state.TagArray[3] = new(HttpClientLoggingTagNames.Method, method);
        state.TagArray[2] = new(HttpClientLoggingTagNames.Host, host);
        state.TagArray[1] = new(HttpClientLoggingTagNames.Path, path);
        state.TagArray[0] = new(OriginalFormat, RequestReadErrorMessage);

        logger.Log(
            LogLevel.Error,
            new(0, nameof(RequestReadError)),
            state,
            exception,
            static (s, _) =>
            {
                var method = s.TagArray[3].Value ?? NullString;
                var host = s.TagArray[2].Value ?? NullString;
                var path = s.TagArray[1].Value ?? NullString;
                return FormattableString.Invariant(
                    $"An error occurred while reading the request data to fill the logger context for request: {method} {host}/{path}");
            });

        state.Clear();
    }

    public static void ResponseReadError(ILogger logger, Exception exception, HttpMethod method, string host, string path)
    {
        var state = LoggerMessageHelper.ThreadLocalState;

        _ = state.ReserveTagSpace(4);
        state.TagArray[3] = new(HttpClientLoggingTagNames.Method, method);
        state.TagArray[2] = new(HttpClientLoggingTagNames.Host, host);
        state.TagArray[1] = new(HttpClientLoggingTagNames.Path, path);
        state.TagArray[0] = new(OriginalFormat, ResponseReadErrorMessage);

        logger.Log(
            LogLevel.Error,
            new(0, nameof(ResponseReadError)),
            state,
            exception,
            static (s, _) =>
            {
                var method = s.TagArray[3].Value ?? NullString;
                var host = s.TagArray[2].Value ?? NullString;
                var path = s.TagArray[1].Value ?? NullString;
                return FormattableString.Invariant(
                    $"An error occurred while reading the response data to fill the logger context for request: {method} {host}/{path}");
            });

        state.Clear();
    }

    public static void LoggerContextMissing(ILogger logger, Exception? exception, string requestState, HttpMethod method, string? host)
    {
        var state = LoggerMessageHelper.ThreadLocalState;

        _ = state.ReserveTagSpace(4);
        state.TagArray[3] = new("RequestState", requestState);
        state.TagArray[2] = new(HttpClientLoggingTagNames.Method, method);
        state.TagArray[1] = new(HttpClientLoggingTagNames.Host, host);
        state.TagArray[0] = new(OriginalFormat, LoggerContextMissingMessage);

        logger.Log(
            LogLevel.Error,
            new(0, nameof(LoggerContextMissing)),
            state,
            exception,
            (s, _) =>
            {
                var requestState = s.TagArray[3].Value ?? NullString;
                var method = s.TagArray[2].Value ?? NullString;
                var host = s.TagArray[1].Value ?? NullString;
                return FormattableString.Invariant($"The logger couldn't read its context for {requestState} request: {method} {host}");
            });

        state.Clear();
    }

    public static void EnrichmentError(ILogger logger, Exception exception, string? enricher, HttpMethod method, string host, string path)
    {
        var state = LoggerMessageHelper.ThreadLocalState;

        _ = state.ReserveTagSpace(5);
        state.TagArray[4] = new("Enricher", enricher);
        state.TagArray[3] = new(HttpClientLoggingTagNames.Method, method);
        state.TagArray[2] = new(HttpClientLoggingTagNames.Host, host);
        state.TagArray[1] = new(HttpClientLoggingTagNames.Path, path);
        state.TagArray[0] = new(OriginalFormat, EnrichmentErrorMessage);

        logger.Log(
            LogLevel.Error,
            new(0, nameof(EnrichmentError)),
            state,
            exception,
            (s, _) =>
            {
                var enricher = s.TagArray[4].Value ?? NullString;
                var method = s.TagArray[3].Value ?? NullString;
                var host = s.TagArray[2].Value ?? NullString;
                var path = s.TagArray[1].Value ?? NullString;
                return FormattableString.Invariant(
                    $"An error occurred in enricher '{enricher}' while enriching the logger context for request: {method} {host}/{path}");
            });

        state.Clear();
    }

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

        var spaceToReserve = MinimalPropertyCount + statusCodePropertyCount + requestHeadersCount + responseHeadersCount +
            record.PathParametersCount + (record.RequestBody is null ? 0 : 1) + (record.ResponseBody is null ? 0 : 1);

        var index = loggerMessageState.ReserveTagSpace(spaceToReserve);
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

        if (record.PathParameters is not null)
        {
            loggerMessageState.AddPathParameters(record.PathParameters, record.PathParametersCount, ref index);
        }

        if (record.RequestBody is not null)
        {
            loggerMessageState.TagArray[index++] = new(HttpClientLoggingTagNames.RequestBody, record.RequestBody);
        }

        if (record.ResponseBody is not null)
        {
            loggerMessageState.TagArray[index++] = new(HttpClientLoggingTagNames.ResponseBody, record.ResponseBody);
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
