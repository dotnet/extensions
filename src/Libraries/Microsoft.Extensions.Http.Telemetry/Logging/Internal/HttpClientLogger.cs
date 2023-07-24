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
        PoolFactory.CreatePool(new LogRecordPooledObjectPolicy());

    private readonly bool _logRequestStart;
    private readonly bool _logResponseHeaders;
    private readonly bool _logRequestHeaders;
    private ILogger<HttpLoggingHandler> _logger;
    private IHttpRequestReader _httpRequestReader;
    private IHttpClientLogEnricher[] _enrichers;

    public HttpClientLogger(
        ILogger<HttpLoggingHandler> logger,
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
    }

    public async ValueTask<object?> LogRequestStartAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var logRecord = _logRecordPool.Get();

        List<KeyValuePair<string, string>>? requestHeadersBuffer = null;

        if (_logRequestHeaders)
        {
            requestHeadersBuffer = _headersPool.Get();
        }

        await _httpRequestReader.ReadRequestAsync(logRecord, request, requestHeadersBuffer, cancellationToken).ConfigureAwait(false);

        if (_logRequestStart)
        {
            Log.OutgoingRequest(_logger, LogLevel.Information, logRecord);
        }

        return logRecord;
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
        throw new NotImplementedException();
    }

    public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
    {
        throw new NotImplementedException();
    }

    public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed)
    {
        throw new NotImplementedException();
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
            // TODO: log an error
            return;
        }

        List<KeyValuePair<string, string>>? responseHeadersBuffer = null;
        if (response is not null)
        {
            if (_logResponseHeaders)
            {
                responseHeadersBuffer = _headersPool.Get();
            }

            await _httpRequestReader.ReadResponseAsync(logRecord, response, responseHeadersBuffer, cancellationToken).ConfigureAwait(false);
        }

        var propertyBag = LogMethodHelper.GetHelper();
        FillLogRecord(logRecord, propertyBag, in elapsed, request, response);

        if (exception is null)
        {
            Log.OutgoingRequest(_logger, GetLogLevel(logRecord), logRecord);
        }
        else
        {
            Log.OutgoingRequestError(_logger, logRecord, exception);
        }

        var requestHeadersBuffer = logRecord.RequestHeaders; // Store the value first, and then return logRecord to the pool.
        _logRecordPool.Return(logRecord);
        LogMethodHelper.ReturnHelper(propertyBag);

        if (responseHeadersBuffer is not null)
        {
            _headersPool.Return(responseHeadersBuffer);
        }

        if (requestHeadersBuffer is not null)
        {
            _headersPool.Return(requestHeadersBuffer);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "We intentionally catch all exception types to make Telemetry code resilient to failures.")]
    private void FillLogRecord(
        LogRecord logRecord, LogMethodHelper propertyBag, in TimeSpan elapsed,
        HttpRequestMessage request, HttpResponseMessage? response)
    {
        foreach (var enricher in _enrichers)
        {
            try
            {
                enricher.Enrich(propertyBag, request, response);
            }
            catch (Exception e)
            {
                Log.EnrichmentError(_logger, e);
            }
        }

        logRecord.EnrichmentProperties = propertyBag;
        logRecord.Duration = (long)elapsed.TotalMilliseconds;
    }
}
