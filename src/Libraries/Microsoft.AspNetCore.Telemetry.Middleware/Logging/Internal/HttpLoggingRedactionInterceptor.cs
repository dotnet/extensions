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

internal sealed class HttpLoggingRedactionInterceptor : IHttpLoggingInterceptor
{
    private const string RequestStartTimestamp = "RequestStartTimestamp";
    private const string Durration = "duration";

    // These three fields are "internal" solely for testing purposes:
    internal TimeProvider TimeProvider = TimeProvider.System;

    private readonly IncomingPathLoggingMode _requestPathLogMode;
    private readonly HttpRouteParameterRedactionMode _parameterRedactionMode;
    private readonly ILogger<HttpLoggingRedactionInterceptor> _logger;
    private readonly IHttpRouteParser _httpRouteParser;
    private readonly IHttpRouteFormatter _httpRouteFormatter;
    private readonly IIncomingHttpRouteUtility _httpRouteUtility;
    private readonly HeaderReader _requestHeadersReader;
    private readonly HeaderReader _responseHeadersReader;
    private readonly string[] _excludePathStartsWith;
    private readonly IHttpLogEnricher[] _enrichers;
    private readonly FrozenDictionary<string, DataClassification> _parametersToRedactMap;

    public HttpLoggingRedactionInterceptor(
        IOptions<LoggingRedactionOptions> options,
        ILogger<HttpLoggingRedactionInterceptor> logger,
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
        else if (logContext.IsAnyEnabled(HttpLoggingFields.Response))
        {
            // We'll need this for the response.
            // TODO: Should we put a state filed on logContext?
            context.Items[RequestStartTimestamp] = TimeProvider.GetTimestamp();
        }

        // Don't enrich if we're not going to log any part of the request
        if (!logContext.IsAnyEnabled(HttpLoggingFields.Request))
        {
            return;
        }

        if (logContext.TryOverride(HttpLoggingFields.RequestPath))
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

            logContext.Add(nameof(request.Path), path);
        }

        if (logContext.TryOverride(HttpLoggingFields.RequestHeaders))
        {
            // TODO: HttpLoggingOptions.Request/ResponseHeaders are ignored which could be confusing.
            // Do we try to reconcile that with LoggingRedactionOptions.RequestHeadersDataClasses?
            _requestHeadersReader.Read(context.Request.Headers, logContext);
        }
    }

    public void OnResponse(HttpLoggingContext logContext)
    {
        // Don't enrich if we're not going to log any part of the response
        if (!logContext.IsAnyEnabled(HttpLoggingFields.Response))
        {
            return;
        }

        var context = logContext.HttpContext;

        if (logContext.TryOverride(HttpLoggingFields.ResponseHeaders))
        {
            _responseHeadersReader.Read(context.Response.Headers, logContext);
        }

        if (_enrichers.Length > 0)
        {
            var enrichmentBag =  LogMethodHelper.GetHelper();
            foreach (var enricher in _enrichers)
            {
                enricher.Enrich(enrichmentBag, context.Request, context.Response);
            }

            foreach (var pair in enrichmentBag)
            {
                logContext.Parameters.Add(pair);
            }
            LogMethodHelper.ReturnHelper(enrichmentBag);
        }

        // Catching duration at the end:
        // Note this does not include the time spent writing the response body.
        var startTime = (long)context.Items[RequestStartTimestamp]!;
        var duration = (long)TimeProvider.GetElapsedTime(startTime, TimeProvider.GetTimestamp()).TotalMilliseconds;
        logContext.Add(Durration, duration);

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
