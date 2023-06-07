// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Telemetry.Internal;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging;

internal sealed class HttpRedactionHandler : IHttpLoggingInterceptor
{
    // These three fields are "internal" solely for testing purposes:
    internal TimeProvider TimeProvider = TimeProvider.System;

    private readonly IncomingPathLoggingMode _requestPathLogMode;
    private readonly HttpRouteParameterRedactionMode _parameterRedactionMode;
    private readonly ILogger<HttpRedactionHandler> _logger;
    private readonly IHttpRouteParser _httpRouteParser;
    private readonly IHttpRouteFormatter _httpRouteFormatter;
    private readonly IIncomingHttpRouteUtility _httpRouteUtility;
    private readonly HeaderReader _requestHeadersReader;
    private readonly HeaderReader _responseHeadersReader;
    private readonly string[] _excludePathStartsWith;
    private readonly IHttpLogEnricher[] _enrichers;
    private readonly FrozenDictionary<string, DataClassification> _parametersToRedactMap;

    public HttpRedactionHandler(
        IOptions<LoggingRedactionOptions> options,
        ILogger<HttpRedactionHandler> logger,
        IEnumerable<IHttpLogEnricher> httpLogEnrichers,
        IHttpRouteParser httpRouteParser,
        IHttpRouteFormatter httpRouteFormatter,
        IRedactorProvider redactorProvider,
        IIncomingHttpRouteUtility httpRouteUtility)
    {
        var optionsValue = options.Value;
        _logger = logger;
        _httpRouteParser = httpRouteParser;
        _httpRouteFormatter = httpRouteFormatter;
        _httpRouteUtility = httpRouteUtility;

        _parametersToRedactMap = optionsValue.RouteParameterDataClasses.ToFrozenDictionary(StringComparer.Ordinal, optimizeForReading: true);

        _requestPathLogMode = EnsureRequestPathLoggingModeIsValid(optionsValue.RequestPathLoggingMode);
        _parameterRedactionMode = optionsValue.RequestPathParameterRedactionMode;

        _requestHeadersReader = new(optionsValue.RequestHeadersDataClasses, redactorProvider);
        _responseHeadersReader = new(optionsValue.ResponseHeadersDataClasses, redactorProvider);

        _excludePathStartsWith = optionsValue.ExcludePathStartsWith.ToArray();

        _enrichers = httpLogEnrichers.ToArray();
    }

    public void OnRequest(HttpLoggingContext logContext)
    {
        var context = logContext.HttpContext;
        var request = context.Request;
        if (ShouldExcludePath(context.Request.Path))
        {
            logContext.LoggingFields = HttpLoggingFields.None;
        }

        // Don't enrich if we're not going to log any part of the request
        if ((HttpLoggingFields.Request & logContext.LoggingFields) == HttpLoggingFields.None)
        {
            return;
        }

        // TODO: Should we put a state filed on logContext?
        context.Items["RequestStartTimestamp"] = TimeProvider.GetTimestamp();

        if (logContext.LoggingFields.HasFlag(HttpLoggingFields.RequestPath))
        {
            string path = TelemetryConstants.Unknown;

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
                    }
                    else
                    {
                        // Case when logging mode is IncomingPathLoggingMode.Structured
                        path = httpRoute;
                        var routeParams = ArrayPool<HttpRouteParameter>.Shared.Rent(routeSegments.ParameterCount);

                        // Setting this value right away to be able to return it back to pool in a callee's "finally" block:
                        if (_httpRouteParser.TryExtractParameters(request.Path, in routeSegments, _parameterRedactionMode, paramsToRedact, ref routeParams))
                        {
                            foreach (var param in routeParams)
                            {
                                logContext.Add(param.Name, param.Value);
                            }
                        }
                    }
                }
            }
            else if (request.Path.HasValue)
            {
                path = request.Path.Value!;
            }

            logContext.Add("path", path);

            // We've handled the path, turn off the default logging
            logContext.LoggingFields &= ~HttpLoggingFields.RequestPath;
        }

        if (logContext.LoggingFields.HasFlag(HttpLoggingFields.RequestHeaders))
        {
            // TODO: HttpLoggingOptions.Request/ResponseHeaders are ignored which could be confusing.
            // Do we try to reconcile that with LoggingRedactionOptions.RequestHeadersDataClasses?
            _requestHeadersReader.Read(context.Request.Headers, logContext);

            // We've handled the request headers, turn off the default logging
            logContext.LoggingFields &= ~HttpLoggingFields.RequestHeaders;
        }
    }

    public void OnResponse(HttpLoggingContext logContext)
    {
        // Don't enrich if we're not going to log any part of the response
        if ((HttpLoggingFields.Response & logContext.LoggingFields) == HttpLoggingFields.None)
        {
            return;
        }

        var context = logContext.HttpContext;

        if (logContext.LoggingFields.HasFlag(HttpLoggingFields.ResponseHeaders))
        {
            _responseHeadersReader.Read(context.Response.Headers, logContext);

            // We've handled the response headers, turn off the default logging
            logContext.LoggingFields &= ~HttpLoggingFields.ResponseHeaders;
        }

        if (_enrichers.Length == 0)
        {
            var enrichmentBag =  LogMethodHelper.GetHelper();
            foreach (var enricher in _enrichers)
            {
                enricher.Enrich(enrichmentBag, context.Request, context.Response);
            }

            foreach (var (key, value) in enrichmentBag)
            {
                logContext.Add(key, value);
            }
        }

        // Catching duration at the end:
        var startTime = (long)context.Items["RequestStartTimestamp"]!;
        var duration = (long)TimeProvider.GetElapsedTime(startTime, TimeProvider.GetTimestamp()).TotalMilliseconds;
        logContext.Add("duration", duration);

        // TODO: What about the exception case?
    }

    private static IncomingPathLoggingMode EnsureRequestPathLoggingModeIsValid(IncomingPathLoggingMode mode)
        => mode switch
        {
            IncomingPathLoggingMode.Structured or IncomingPathLoggingMode.Formatted => mode,
            _ => throw new InvalidOperationException($"Unsupported value '{mode}' for enum type '{nameof(IncomingPathLoggingMode)}'"),
        };

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

#endif
