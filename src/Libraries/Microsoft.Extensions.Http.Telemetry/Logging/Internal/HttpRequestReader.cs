// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Internal;

internal sealed class HttpRequestReader : IHttpRequestReader
{
    private readonly IHttpRouteFormatter _routeFormatter;
    private readonly IHttpHeadersReader _httpHeadersReader;
    private readonly FrozenDictionary<string, DataClassification> _defaultSensitiveParameters;

    private readonly bool _logRequestBody;
    private readonly bool _logResponseBody;

    private readonly bool _logRequestHeaders;
    private readonly bool _logResponseHeaders;

    private readonly HttpRouteParameterRedactionMode _routeParameterRedactionMode;

    // These are not registered in DI as handler today is public and we would need to make all of those types public.
    // They are not implemented as statics to simplify design and pass less arguments around.
    // Also wanted to encapsulate logic of reading each part of the request to simplify handler logic itself.
    private readonly HttpRequestBodyReader _httpRequestBodyReader;
    private readonly HttpResponseBodyReader _httpResponseBodyReader;

    private readonly OutgoingPathLoggingMode _outgoingPathLogMode;
    private readonly IOutgoingRequestContext _requestMetadataContext;
    private readonly IDownstreamDependencyMetadataManager? _downstreamDependencyMetadataManager;

    public HttpRequestReader(
        IServiceProvider serviceProvider,
        IOptionsMonitor<LoggingOptions> optionsMonitor,
        IHttpRouteFormatter routeFormatter,
        IOutgoingRequestContext requestMetadataContext,
        IDownstreamDependencyMetadataManager? downstreamDependencyMetadataManager = null,
        [ServiceKey] string? serviceKey = null)
        : this(
              optionsMonitor.GetKeyedOrCurrent(serviceKey),
              routeFormatter,
              serviceProvider.GetRequiredOrKeyedService<IHttpHeadersReader>(serviceKey),
              requestMetadataContext,
              downstreamDependencyMetadataManager)
    {
    }

    internal HttpRequestReader(
        LoggingOptions options,
        IHttpRouteFormatter routeFormatter,
        IHttpHeadersReader httpHeadersReader,
        IOutgoingRequestContext requestMetadataContext,
        IDownstreamDependencyMetadataManager? downstreamDependencyMetadataManager = null)
    {
        _outgoingPathLogMode = Throw.IfOutOfRange(options.RequestPathLoggingMode);
        _httpHeadersReader = httpHeadersReader;

        _routeFormatter = routeFormatter;
        _requestMetadataContext = requestMetadataContext;
        _downstreamDependencyMetadataManager = downstreamDependencyMetadataManager;

        _defaultSensitiveParameters = options.RouteParameterDataClasses.ToFrozenDictionary(StringComparer.Ordinal);

        if (options.LogBody)
        {
            _logRequestBody = options.RequestBodyContentTypes.Count > 0;
            _logResponseBody = options.ResponseBodyContentTypes.Count > 0;
        }

        _logRequestHeaders = options.RequestHeadersDataClasses.Count > 0;
        _logResponseHeaders = options.ResponseHeadersDataClasses.Count > 0;

        _httpRequestBodyReader = new HttpRequestBodyReader(options);
        _httpResponseBodyReader = new HttpResponseBodyReader(options);

        _routeParameterRedactionMode = options.RequestPathParameterRedactionMode;
    }

    public async Task ReadRequestAsync(LogRecord logRecord, HttpRequestMessage request,
        List<KeyValuePair<string, string>>? requestHeadersBuffer, CancellationToken cancellationToken)
    {
        logRecord.Host = request.RequestUri?.Host ?? TelemetryConstants.Unknown;
        logRecord.Method = request.Method;
        logRecord.Path = GetRedactedPath(request);

        if (_logRequestHeaders)
        {
            _httpHeadersReader.ReadRequestHeaders(request, requestHeadersBuffer);
            logRecord.RequestHeaders = requestHeadersBuffer;
        }

        if (_logRequestBody)
        {
            logRecord.RequestBody = await _httpRequestBodyReader.ReadAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public async Task ReadResponseAsync(LogRecord logRecord, HttpResponseMessage response,
        List<KeyValuePair<string, string>>? responseHeadersBuffer,
        CancellationToken cancellationToken)
    {
        if (_logResponseHeaders)
        {
            _httpHeadersReader.ReadResponseHeaders(response, responseHeadersBuffer);
            logRecord.ResponseHeaders = responseHeadersBuffer;
        }

        if (_logResponseBody)
        {
            logRecord.ResponseBody = await _httpResponseBodyReader.ReadAsync(response, cancellationToken).ConfigureAwait(false);
        }

        logRecord.StatusCode = (int)response.StatusCode;
    }

    private string GetRedactedPath(HttpRequestMessage request)
    {
        if (request.RequestUri is null)
        {
            return TelemetryConstants.Unknown;
        }

        if (_routeParameterRedactionMode == HttpRouteParameterRedactionMode.None)
        {
            return request.RequestUri.AbsolutePath;
        }

        var requestMetadata = request.GetRequestMetadata() ??
            _requestMetadataContext.RequestMetadata ??
            _downstreamDependencyMetadataManager?.GetRequestMetadata(request);

        if (requestMetadata == null)
        {
            return TelemetryConstants.Redacted;
        }

        var route = requestMetadata.RequestRoute;
        if (route == TelemetryConstants.Unknown)
        {
            return requestMetadata.RequestName;
        }

        return _outgoingPathLogMode switch
        {
            OutgoingPathLoggingMode.Formatted => _routeFormatter.Format(route, request.RequestUri.AbsolutePath, _routeParameterRedactionMode, _defaultSensitiveParameters),
            _ => route
        };
    }
}
