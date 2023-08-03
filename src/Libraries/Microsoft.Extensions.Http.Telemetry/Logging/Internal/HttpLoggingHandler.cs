// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Logging;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Internal;

/// <summary>
/// Handler that logs HTTP client requests./>.
/// </summary>
internal sealed class HttpLoggingHandler : DelegatingHandler
{
    internal TimeProvider TimeProvider = TimeProvider.System;

    private readonly IHttpClientLogEnricher[] _enrichers;
    private readonly ILogger<HttpLoggingHandler> _logger;
    private readonly IHttpRequestReader _httpRequestReader;

    private readonly ObjectPool<List<KeyValuePair<string, string>>> _headersPool =
        PoolFactory.CreateListPool<KeyValuePair<string, string>>();

    private readonly ObjectPool<LogRecord> _logRecordPool =
        PoolFactory.CreatePool(new LogRecordPooledObjectPolicy());

    private readonly bool _logRequestStart;
    private readonly bool _logRequestHeaders;
    private readonly bool _logResponseHeaders;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpLoggingHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpRequestReader">Handler for reading an HTTP request message.</param>
    /// <param name="enrichers">HTTP client log enrichers to enrich log records by.</param>
    /// <param name="options">An instance of <see cref="LoggingOptions"/> representing HTTP logging options.</param>
    public HttpLoggingHandler(
        ILogger<HttpLoggingHandler> logger,
        IHttpRequestReader httpRequestReader,
        IEnumerable<IHttpClientLogEnricher> enrichers,
        IOptions<LoggingOptions> options)
    {
        _logger = logger;
        _httpRequestReader = httpRequestReader;
        _enrichers = enrichers.ToArray();
        _ = Throw.IfMemberNull(options, options.Value);

        _logRequestStart = options.Value.LogRequestStart;
        _logResponseHeaders = options.Value.ResponseHeadersDataClasses.Count > 0;
        _logRequestHeaders = options.Value.RequestHeadersDataClasses.Count > 0;
    }

    /// <summary>
    /// Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.
    /// </summary>
    /// <param name="request">The HTTP request message to send to the server.</param>
    /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
    /// <returns>
    /// The task object representing the asynchronous operation.
    /// </returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(request);

        var timestamp = TimeProvider.GetTimestamp();

        var logRecord = _logRecordPool.Get();
        var propertyBag = LogMethodHelper.GetHelper();

        List<KeyValuePair<string, string>>? requestHeadersBuffer = null;
        List<KeyValuePair<string, string>>? responseHeadersBuffer = null;

        HttpResponseMessage? response = null;

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

            response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (_logResponseHeaders)
            {
                responseHeadersBuffer = _headersPool.Get();
            }

            await _httpRequestReader.ReadResponseAsync(logRecord, response, responseHeadersBuffer, cancellationToken).ConfigureAwait(false);

            FillLogRecord(logRecord, propertyBag, timestamp, request, response);
            Log.OutgoingRequest(_logger, GetLogLevel(logRecord), logRecord);

            return response;
        }
        catch (Exception exception)
        {
            FillLogRecord(logRecord, propertyBag, timestamp, request, response);
            Log.OutgoingRequestError(_logger, logRecord, exception);

            throw;
        }
        finally
        {
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

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "We intentionally catch all exception types to make Telemetry code resilient to failures.")]
    private void FillLogRecord(
        LogRecord logRecord, LogMethodHelper propertyBag, long timestamp,
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

        logRecord.EnrichmentTags = propertyBag;
        logRecord.Duration = (long)TimeProvider.GetElapsedTime(timestamp, TimeProvider.GetTimestamp()).TotalMilliseconds;
    }
}
