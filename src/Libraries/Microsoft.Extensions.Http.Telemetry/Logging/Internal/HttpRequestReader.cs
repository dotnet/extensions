// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Compliance.Classification;
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
        IOptions<LoggingOptions> options,
        IHttpRouteFormatter routeFormatter,
        IHttpHeadersReader httpHeadersReader,
        IOutgoingRequestContext requestMetadataContext,
        IDownstreamDependencyMetadataManager? downstreamDependencyMetadataManager = null)
    {
        var optionsValue = Throw.IfMemberNull(options, options.Value);
        _routeFormatter = routeFormatter;
        _outgoingPathLogMode = Throw.IfOutOfRange(optionsValue.RequestPathLoggingMode);
        _httpHeadersReader = httpHeadersReader;
        _requestMetadataContext = requestMetadataContext;
        _downstreamDependencyMetadataManager = downstreamDependencyMetadataManager;

        _defaultSensitiveParameters = optionsValue.RouteParameterDataClasses.ToFrozenDictionary(StringComparer.Ordinal, optimizeForReading: true);

        if (optionsValue.LogBody)
        {
            _logRequestBody = optionsValue.RequestBodyContentTypes.Count > 0;
            _logResponseBody = optionsValue.ResponseBodyContentTypes.Count > 0;
        }

        _logRequestHeaders = optionsValue.RequestHeadersDataClasses.Count > 0;
        _logResponseHeaders = optionsValue.ResponseHeadersDataClasses.Count > 0;

        _httpRequestBodyReader = new HttpRequestBodyReader(options);
        _httpResponseBodyReader = new HttpResponseBodyReader(options);

        _routeParameterRedactionMode = optionsValue.RequestPathParameterRedactionMode;
    }

    public async Task ReadRequestAsync(LogRecord logRecord, HttpRequestMessage request,
        List<KeyValuePair<string, string>>? requestHeadersBuffer, CancellationToken cancellationToken)
    {
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

        logRecord.Host = request.RequestUri?.Host ?? TelemetryConstants.Unknown;
        logRecord.Method = request.Method;
        logRecord.Path = GetRedactedPath(request);
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
