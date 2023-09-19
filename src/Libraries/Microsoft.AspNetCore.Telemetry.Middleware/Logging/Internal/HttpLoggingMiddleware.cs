// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Telemetry.Internal;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

// Keeping this namespace so that users are able to control logging:
namespace Microsoft.AspNetCore.Telemetry.Http.Logging;

internal sealed class HttpLoggingMiddleware : IMiddleware
{
    // These three fields are "internal" solely for testing purposes:
    internal int BodyReadSizeLimit;
    internal TimeProvider TimeProvider = TimeProvider.System;
    internal Func<ResponseInterceptingStream, ReadOnlyMemory<byte>> GetResponseBodyInterceptedData = static stream => stream.GetInterceptedSequence();
    internal TimeSpan RequestBodyReadTimeout;

    private readonly bool _logRequestStart;
    private readonly bool _logRequestBody;
    private readonly bool _logResponseBody;
    private readonly bool _logRequestHeaders;
    private readonly bool _logResponseHeaders;

    private readonly IncomingPathLoggingMode _requestPathLogMode;
    private readonly HttpRouteParameterRedactionMode _parameterRedactionMode;
    private readonly ILogger<HttpLoggingMiddleware> _logger;
    private readonly IHttpRouteParser _httpRouteParser;
    private readonly IHttpRouteFormatter _httpRouteFormatter;
    private readonly IIncomingHttpRouteUtility _httpRouteUtility;
    private readonly HeaderReader _requestHeadersReader;
    private readonly HeaderReader _responseHeadersReader;
    private readonly string[] _excludePathStartsWith;
    private readonly IHttpLogEnricher[] _enrichers;
    private readonly MediaType[] _requestMediaTypes;
    private readonly MediaType[] _responseMediaTypes;
    private readonly FrozenDictionary<string, DataClassification> _parametersToRedactMap;

    private readonly ObjectPool<IncomingRequestLogRecord> _logRecordPool =
        PoolFactory.CreatePool<IncomingRequestLogRecord>();

    private readonly ObjectPool<List<KeyValuePair<string, string>>> _headersPool =
        PoolFactory.CreateListPool<KeyValuePair<string, string>>();

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Technical debt accepted.")]
    public HttpLoggingMiddleware(
        IOptions<LoggingOptions> options,
        ILogger<HttpLoggingMiddleware> logger,
        IEnumerable<IHttpLogEnricher> httpLogEnrichers,
        IHttpRouteParser httpRouteParser,
        IHttpRouteFormatter httpRouteFormatter,
        IRedactorProvider redactorProvider,
        IIncomingHttpRouteUtility httpRouteUtility,
        IDebuggerState? debugger = null)
    {
        var optionsValue = options.Value;
        _logger = logger;
        _httpRouteParser = httpRouteParser;
        _httpRouteFormatter = httpRouteFormatter;
        _httpRouteUtility = httpRouteUtility;
        _logRequestStart = optionsValue.LogRequestStart;
        if (optionsValue.LogBody)
        {
            _logRequestBody = optionsValue.RequestBodyContentTypes.Count > 0;
            _logResponseBody = optionsValue.ResponseBodyContentTypes.Count > 0;
        }

        _parametersToRedactMap = optionsValue.RouteParameterDataClasses.ToFrozenDictionary(StringComparer.Ordinal);

        _requestPathLogMode = EnsureRequestPathLoggingModeIsValid(optionsValue.RequestPathLoggingMode);
        _parameterRedactionMode = optionsValue.RequestPathParameterRedactionMode;

        BodyReadSizeLimit = optionsValue.BodySizeLimit;

        debugger ??= DebuggerState.System;
        RequestBodyReadTimeout = debugger.IsAttached
            ? Timeout.InfiniteTimeSpan
            : optionsValue.RequestBodyReadTimeout;

        _requestMediaTypes = optionsValue.RequestBodyContentTypes
            .Select(static x => new MediaType(x))
            .ToArray();

        _responseMediaTypes = optionsValue.ResponseBodyContentTypes
            .Select(static x => new MediaType(x))
            .ToArray();

        _logRequestHeaders = optionsValue.RequestHeadersDataClasses.Count > 0;
        _logResponseHeaders = optionsValue.ResponseHeadersDataClasses.Count > 0;
        _requestHeadersReader = new(optionsValue.RequestHeadersDataClasses, redactorProvider);
        _responseHeadersReader = new(optionsValue.ResponseHeadersDataClasses, redactorProvider);

        _excludePathStartsWith = optionsValue.ExcludePathStartsWith.ToArray();

        _enrichers = httpLogEnrichers.ToArray();

        // There's no need to use this middleware,
        // so log a warning and hope that "LogLevel.Warning" is enabled:
        if (!_logger.IsEnabled(Log.DefaultLogLevel))
        {
            _logger.MiddlewareIsMisused(Log.DefaultLogLevel, nameof(HttpLoggingServiceExtensions.UseHttpLoggingMiddleware));
        }
    }

    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (ShouldExcludePath(context.Request.Path))
        {
            return next(context);
        }
        else
        {
            return InvokeAsyncWithPathAsync(context, next);
        }
    }

    private static IncomingPathLoggingMode EnsureRequestPathLoggingModeIsValid(IncomingPathLoggingMode mode)
        => mode switch
        {
            IncomingPathLoggingMode.Structured or IncomingPathLoggingMode.Formatted => mode,
            _ => throw new InvalidOperationException($"Unsupported value '{mode}' for enum type '{nameof(IncomingPathLoggingMode)}'"),
        };

    private async Task InvokeAsyncWithPathAsync(HttpContext context, RequestDelegate next)
    {
        ResponseInterceptingStream? bufferingResponseStream = null;
        string? requestBody = null;
        var timestamp = TimeProvider.GetTimestamp();
        try
        {
            if (_logResponseBody)
            {
                // Swapping response stream:
                var oldFeature = context.Features.Get<IHttpResponseBodyFeature>()!;
                bufferingResponseStream = ResponseInterceptingStreamPool.Get(oldFeature, BodyReadSizeLimit);
                context.Features.Set<IHttpResponseBodyFeature>(bufferingResponseStream);
            }

            requestBody = _logRequestBody
                ? await GetRequestBodyAsync(context.Request, context.RequestAborted).ConfigureAwait(false)
                : null;

            if (_logRequestStart)
            {
                LogRequest(context, timestamp, isRequestStart: true, requestBody, responseBody: null);
            }

            await next(context).ConfigureAwait(false);

            var responseBody = _logResponseBody
                ? GetResponseBody(context.Response, bufferingResponseStream!)
                : null;

            context.Response.OnCompleted(() =>
            {
                LogRequest(context, timestamp, isRequestStart: false, requestBody, responseBody);

                return Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            // Even if the response body has been already read, we can re-read it safely:
            var responseBody = _logResponseBody
                ? GetResponseBody(context.Response, bufferingResponseStream!)
                : null;

            LogRequest(context, timestamp, isRequestStart: false, requestBody, responseBody, ex);

            throw;
        }
        finally
        {
            if (bufferingResponseStream is not null)
            {
                context.Features.Set(bufferingResponseStream.InnerBodyFeature);
                ResponseInterceptingStreamPool.Return(bufferingResponseStream);
            }
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Intentional")]
    private string? GetResponseBody(HttpResponse response, ResponseInterceptingStream stream)
    {
        if (!MediaTypeSetExtensions.Covers(_responseMediaTypes, response.ContentType))
        {
            return null;
        }

        try
        {
            var sequenceSpan = GetResponseBodyInterceptedData(stream).Span;

            return Encoding.UTF8.GetString(sequenceSpan);
        }
        catch (Exception ex)
        {
            // We are intentionally catching and logging any exceptions which may happen.
            _logger.ErrorReadingResponseBody(ex);
            return null;
        }
    }

    private void LogRequest(
        HttpContext context,
        long timestamp,
        bool isRequestStart,
        string? requestBody,
        string? responseBody,
        Exception? exception = null)
    {
        const int StatusCodeOnException = 0;
        const int LowestUnsuccessfulStatusCode = 400;

        // Don't get a tag collector for "RequestStart" log record:
        var collector = isRequestStart || _enrichers.Length == 0
            ? null
            : LogMethodHelper.GetHelper();

        var requestHeaders = _logRequestHeaders
            ? _headersPool.Get()
            : null;

        // Checking response headers, since we can possibly don't have a response:
        var responseHeaders = _logResponseHeaders && context.Response.Headers.Count > 0
            ? _headersPool.Get()
            : null;

        var logRecord = _logRecordPool.Get();
        try
        {
            logRecord.RequestBody = requestBody;
            logRecord.ResponseBody = responseBody;
            logRecord.RequestHeaders = requestHeaders;
            logRecord.ResponseHeaders = responseHeaders;

            if (requestHeaders != null)
            {
                _requestHeadersReader.Read(context.Request.Headers, requestHeaders);
            }

            if (responseHeaders != null)
            {
                _responseHeadersReader.Read(context.Response.Headers, responseHeaders);
            }

            FillLogRecord(logRecord, context, collector);

            if (isRequestStart)
            {
                // Don't emit both status code and duration tags on request start:
                logRecord.Duration = null;
                logRecord.StatusCode = null;
            }
            else
            {
                // Catching duration at the end:
                logRecord.Duration = (long)TimeProvider.GetElapsedTime(timestamp).TotalMilliseconds;
            }

            if (exception == null)
            {
                _logger.IncomingRequest(logRecord);
            }
            else
            {
                // Logging status code == 0 when exception occurs and no middleware has set a meaningful status code:
                if (logRecord.StatusCode < LowestUnsuccessfulStatusCode)
                {
                    logRecord.StatusCode = StatusCodeOnException;
                }

                _logger.RequestProcessingError(exception, logRecord);
            }
        }
        finally
        {
            if (logRecord.PathParameters != null)
            {
                ArrayPool<HttpRouteParameter>.Shared.Return(logRecord.PathParameters);
                logRecord.PathParameters = null;
            }

            if (collector != null)
            {
                LogMethodHelper.ReturnHelper(collector);
                logRecord.EnrichmentPropertyBag = null;
            }

            if (requestHeaders != null)
            {
                _headersPool.Return(requestHeaders);
                logRecord.RequestHeaders = null;
            }

            if (responseHeaders != null)
            {
                _headersPool.Return(responseHeaders);
                logRecord.ResponseHeaders = null;
            }

            // Return log record at the end:
            _logRecordPool.Return(logRecord);
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Intentional")]
    private async Task<string?> GetRequestBodyAsync(HttpRequest request, CancellationToken token)
    {
        if (!MediaTypeSetExtensions.Covers(_requestMediaTypes, request.ContentType))
        {
            return null;
        }

        try
        {
            var sequence =
                await request.ReadBodyAsync(RequestBodyReadTimeout, BodyReadSizeLimit, token)
                    .ConfigureAwait(false);

#if NET5_0_OR_GREATER
            var stringifiedBody = Encoding.UTF8.GetString(in sequence);
#else
            string stringifiedBody;
            if (sequence.IsSingleSegment)
            {
                stringifiedBody = Encoding.UTF8.GetString(sequence.FirstSpan);
            }
            else
            {
                var buffer = ArrayPool<byte>.Shared.Rent((int)sequence.Length);
                try
                {
                    sequence.CopyTo(buffer);
                    stringifiedBody = Encoding.UTF8.GetString(buffer.AsSpan(0, (int)sequence.Length));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
#endif

            return stringifiedBody;
        }
        catch (OperationCanceledException)
        {
            // Rethrow cancellation exceptions.
            throw;
        }
        catch (Exception ex)
        {
            // We are intentionally catching and logging any exceptions which may happen.
            _logger.ErrorReadingRequestBody(ex);
            return null;
        }
    }

    private void FillLogRecord(
        IncomingRequestLogRecord logRecord,
        HttpContext context,
        LogMethodHelper? collector)
    {
        var request = context.Request;
        var response = context.Response;

        string path = TelemetryConstants.Unknown;
        var pathParamsCount = 0;

        if (_parameterRedactionMode != HttpRouteParameterRedactionMode.None)
        {
            var endpoint = context.GetEndpoint() as RouteEndpoint;

            if (endpoint?.RoutePattern.RawText != null)
            {
                var httpRoute = endpoint.RoutePattern.RawText;
                var paramsToRedact = _httpRouteUtility.GetSensitiveParameters(httpRoute, request, _parametersToRedactMap);

                var routeSegments = _httpRouteParser.ParseRoute(httpRoute);

                if (_requestPathLogMode == IncomingPathLoggingMode.Formatted)
                {
                    path = _httpRouteFormatter.Format(in routeSegments, request.Path, _parameterRedactionMode, paramsToRedact);
                    logRecord.PathParameters = null;
                }
                else
                {
                    // Case when logging mode is IncomingPathLoggingMode.Structured
                    path = httpRoute;
                    var routeParams = ArrayPool<HttpRouteParameter>.Shared.Rent(routeSegments.ParameterCount);

                    // Setting this value right away to be able to return it back to pool in a callee's "finally" block:
                    logRecord.PathParameters = routeParams;
                    if (_httpRouteParser.TryExtractParameters(request.Path, in routeSegments, _parameterRedactionMode, paramsToRedact, ref routeParams))
                    {
                        pathParamsCount = routeSegments.ParameterCount;
                    }
                }
            }
            else
            {
                logRecord.PathParameters = null;
            }
        }
        else if (request.Path.HasValue)
        {
            path = request.Path.Value!;
        }

        // We need to set all the values (logRecord was taken from the pool):
        logRecord.Path = path;
        logRecord.PathParametersCount = pathParamsCount;
        logRecord.Method = request.Method;
        logRecord.StatusCode = response.StatusCode;
        logRecord.Host = request.Host.Value;

        if (collector != null)
        {
            foreach (var enricher in _enrichers)
            {
                enricher.Enrich(collector, request, response);
            }
        }

        logRecord.EnrichmentPropertyBag = collector;
    }

    private bool ShouldExcludePath(string path)
    {
        foreach (var excludedPath in _excludePathStartsWith)
        {
            if (path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
