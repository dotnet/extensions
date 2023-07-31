// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Http.Telemetry.Logging.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Logging;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Http.Telemetry.Logging;

internal sealed class HttpClientLogger : IHttpClientAsyncLogger
{
    private readonly ObjectPool<List<KeyValuePair<string, string>>> _headersPool =
        PoolFactory.CreateListPool<KeyValuePair<string, string>>();

    private readonly ObjectPool<LogRecord> _logRecordPool =
        PoolFactory.CreateResettingPool<LogRecord>();

    private readonly bool _logRequestStart;
    private readonly bool _logResponseHeaders;
    private readonly bool _logRequestHeaders;
    private readonly bool _pathParametersRedactionSkipped;
    private ILogger<HttpClientLogger> _logger;
    private IHttpRequestReader _httpRequestReader;
    private IHttpClientLogEnricher[] _enrichers;

    public HttpClientLogger(
        ILogger<HttpClientLogger> logger,
        IHttpRequestReader httpRequestReader,
        IEnumerable<IHttpClientLogEnricher> enrichers,
        IOptions<LoggingOptions> options)
    {
        _logger = logger;
        _httpRequestReader = httpRequestReader;
        _enrichers = enrichers.ToArray();
        var optionsValue = Throw.IfMemberNull(options, options.Value);

        _logRequestStart = optionsValue.LogRequestStart;
        _logResponseHeaders = optionsValue.ResponseHeadersDataClasses.Count > 0;
        _logRequestHeaders = optionsValue.RequestHeadersDataClasses.Count > 0;
        _pathParametersRedactionSkipped = optionsValue.RequestPathParameterRedactionMode == HttpRouteParameterRedactionMode.None;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The logger shouldn't throw")]
    public async ValueTask<object?> LogRequestStartAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var logRecord = _logRecordPool.Get();

        List<KeyValuePair<string, string>>? requestHeadersBuffer = null;
        if (_logRequestHeaders)
        {
            requestHeadersBuffer = _headersPool.Get();
        }

        try
        {
            await _httpRequestReader.ReadRequestAsync(logRecord, request, requestHeadersBuffer, cancellationToken).ConfigureAwait(false);

            if (_logRequestStart)
            {
                Log.OutgoingRequest(_logger, LogLevel.Information, logRecord);
            }

            return logRecord;
        }
        catch (Exception ex)
        {
            // If redaction is skipped, we can log unredacted request path; otherwise use "logRecord.Path" (even though it might not be set):
            var pathToLog = _pathParametersRedactionSkipped
                ? request.RequestUri?.AbsolutePath
                : logRecord.Path;

            Log.RequestReadError(_logger, ex, request.Method, request.RequestUri?.Host, pathToLog ?? string.Empty);

            // Return back pooled objects (since the logRecord wasn't fully prepared):
            _logRecordPool.Return(logRecord);

            if (requestHeadersBuffer is not null)
            {
                _headersPool.Return(requestHeadersBuffer);
            }

            // Recommendation is to swallow the exception (logger shouldn't throw), so we don't re-throw here:
            return null;
        }
    }

    public async ValueTask LogRequestStopAsync(
        object? context,
        HttpRequestMessage request,
        HttpResponseMessage response,
        TimeSpan elapsed,
        CancellationToken cancellationToken = default)
            => await LogResponseAsync(context, request, response, null, elapsed, cancellationToken).ConfigureAwait(false);

    public async ValueTask LogRequestFailedAsync(
        object? context,
        HttpRequestMessage request,
        HttpResponseMessage? response,
        Exception exception,
        TimeSpan elapsed,
        CancellationToken cancellationToken = default)
            => await LogResponseAsync(context, request, response, exception, elapsed, cancellationToken).ConfigureAwait(false);

    public object? LogRequestStart(HttpRequestMessage request)
    {
        throw new NotSupportedException();
    }

    public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
    {
        throw new NotSupportedException();
    }

    public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed)
    {
        throw new NotSupportedException();
    }

    private static LogLevel GetLogLevel(LogRecord logRecord)
    {
        const int HttpErrorsRangeStart = 400;
        const int HttpErrorsRangeEnd = 599;
        int statusCode = logRecord.StatusCode!.Value;

        if (statusCode >= HttpErrorsRangeStart && statusCode <= HttpErrorsRangeEnd)
        {
            return LogLevel.Error;
        }

        return LogLevel.Information;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The logger shouldn't throw")]
    private async ValueTask LogResponseAsync(
        object? context,
        HttpRequestMessage request,
        HttpResponseMessage? response,
        Exception? exception,
        TimeSpan elapsed,
        CancellationToken cancellationToken)
    {
        if (context is not LogRecord logRecord)
        {
            // TODO: we need to decide - log an error or try to load the log record from the context?
            return;
        }

        LogMethodHelper? propertyBag = null;
        List<KeyValuePair<string, string>>? responseHeadersBuffer = null;
        try
        {
            if (response is not null)
            {
                if (_logResponseHeaders)
                {
                    responseHeadersBuffer = _headersPool.Get();
                }

                await _httpRequestReader.ReadResponseAsync(logRecord, response, responseHeadersBuffer, cancellationToken).ConfigureAwait(false);
            }

            propertyBag = LogMethodHelper.GetHelper();
            FillLogRecord(logRecord, propertyBag, in elapsed, request, response, exception);

            if (exception is null)
            {
                Log.OutgoingRequest(_logger, GetLogLevel(logRecord), logRecord);
            }
            else
            {
                Log.OutgoingRequestError(_logger, logRecord, exception);
            }
        }
        catch (Exception ex)
        {
            // Logger shouldn't throw, so we just log the exception and don't re-throw it:
            Log.ResponseReadError(_logger, ex, request.Method, logRecord.Host, logRecord.Path);
        }
        finally
        {
            var requestHeadersBuffer = logRecord.RequestHeaders; // Store the value first, and then return logRecord to the pool.
            _logRecordPool.Return(logRecord);

            if (propertyBag is not null)
            {
                LogMethodHelper.ReturnHelper(propertyBag);
            }

            if (responseHeadersBuffer is not null)
            {
                _headersPool.Return(responseHeadersBuffer);
            }

            if (requestHeadersBuffer is not null)
            {
                _headersPool.Return(requestHeadersBuffer);
            }
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "We intentionally catch all exception types to make Telemetry code resilient to failures.")]
    private void FillLogRecord(
        LogRecord logRecord, LogMethodHelper propertyBag, in TimeSpan elapsed,
        HttpRequestMessage request, HttpResponseMessage? response, Exception? exception)
    {
        foreach (var enricher in _enrichers)
        {
            try
            {
                enricher.Enrich(propertyBag, request, response, exception);
            }
            catch (Exception e)
            {
                Log.EnrichmentError(_logger, e, request.Method, logRecord.Host, logRecord.Path);
            }
        }

        logRecord.EnrichmentProperties = propertyBag;
        logRecord.Duration = (long)elapsed.TotalMilliseconds;
    }
}
