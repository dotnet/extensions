// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET8_0_OR_GREATER

using System;
using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

internal sealed class HttpLoggingRedactionInterceptor : IHttpLoggingInterceptor
{
    private readonly IHttpLogEnricher[] _enrichers;
    private readonly IncomingPathLoggingMode _requestPathLogMode;
    private readonly HttpRouteParameterRedactionMode _parameterRedactionMode;
    private readonly ILogger<HttpLoggingRedactionInterceptor> _logger;
    private readonly IHttpRouteParser _httpRouteParser;
    private readonly IHttpRouteFormatter _httpRouteFormatter;
    private readonly IIncomingHttpRouteUtility _httpRouteUtility;
    private readonly HeaderReader _requestHeadersReader;
    private readonly HeaderReader _responseHeadersReader;
    private readonly string[] _excludePathStartsWith;
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
        _enrichers = httpLogEnrichers.ToArray();
        _httpRouteParser = httpRouteParser;
        _httpRouteFormatter = httpRouteFormatter;
        _httpRouteUtility = httpRouteUtility;

        _parametersToRedactMap = optionsValue.RouteParameterDataClasses.ToFrozenDictionary(StringComparer.Ordinal);

        _requestPathLogMode = EnsureRequestPathLoggingModeIsValid(optionsValue.RequestPathLoggingMode);
        _parameterRedactionMode = optionsValue.RequestPathParameterRedactionMode;

        _requestHeadersReader = new(optionsValue.RequestHeadersDataClasses, redactorProvider);
        _responseHeadersReader = new(optionsValue.ResponseHeadersDataClasses, redactorProvider);

        _excludePathStartsWith = optionsValue.ExcludePathStartsWith.ToArray();
    }

    public ValueTask OnRequestAsync(HttpLoggingInterceptorContext logContext)
    {
        var context = logContext.HttpContext;
        var request = context.Request;
        if (_excludePathStartsWith.Length != 0 && ShouldExcludePath(context.Request.Path.Value!))
        {
            logContext.LoggingFields = HttpLoggingFields.None;
            return default;
        }

        // Don't redact if we're not going to log any part of the request
        if (!logContext.IsAnyEnabled(HttpLoggingFields.RequestPropertiesAndHeaders))
        {
            return default;
        }

        // Always included, redaction will filter it out of the headers by default.
        logContext.AddParameter(HttpLoggingTagNames.Host, context.Request.Host.Value);

        if (logContext.TryDisable(HttpLoggingFields.RequestPath))
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
                            for (var i = 0; i < routeSegments.ParameterCount; i++)
                            {
                                logContext.AddParameter(routeParams[i].Name, routeParams[i].Value);
                            }
                        }
                    }
                }
            }
            else if (request.Path.HasValue)
            {
                path = request.Path.Value!;
            }

            logContext.AddParameter(nameof(request.Path), path);
        }

        if (logContext.TryDisable(HttpLoggingFields.RequestHeaders))
        {
            _requestHeadersReader.Read(context.Request.Headers, logContext.Parameters, HttpLoggingTagNames.RequestHeaderPrefix);
        }

        return default;
    }

    public ValueTask OnResponseAsync(HttpLoggingInterceptorContext logContext)
    {
        var context = logContext.HttpContext;

        if (logContext.TryDisable(HttpLoggingFields.ResponseHeaders))
        {
            _responseHeadersReader.Read(context.Response.Headers, logContext.Parameters, HttpLoggingTagNames.ResponseHeaderPrefix);
        }

        // Don't enrich if we're not going to log any part of the response
        if (_enrichers.Length == 0
            || (!logContext.IsAnyEnabled(HttpLoggingFields.Response) && logContext.Parameters.Count == 0))
        {
            return default;
        }

        var loggerMessageState = LoggerMessageHelper.ThreadLocalState;

        try
        {
            foreach (var enricher in _enrichers)
            {
#pragma warning disable CA1031 // Do not catch general exception types
                try
                {
                    enricher.Enrich(loggerMessageState, context);
                }
                catch (Exception ex)
                {
                    _logger.EnricherFailed(ex, enricher.GetType().Name);
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }

            foreach (var pair in loggerMessageState)
            {
                logContext.Parameters.Add(pair);
            }
        }
        finally
        {
            loggerMessageState.Clear();
        }

        return default;
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
