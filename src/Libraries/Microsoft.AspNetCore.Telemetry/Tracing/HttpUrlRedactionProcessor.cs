// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Telemetry.Internal;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Telemetry;

internal sealed class HttpUrlRedactionProcessor
{
    private readonly ILogger<HttpUrlRedactionProcessor> _logger;
    private readonly HttpTracingOptions _options;
    private readonly IHttpRouteFormatter _routeFormatter;
    private readonly IHttpRouteParser _routeParser;
    private readonly IIncomingHttpRouteUtility _routeUtility;
    private readonly ConcurrentDictionary<(string scheme, string host, string path), string> _urlCache = new();
    private readonly FrozenDictionary<string, DataClassification> _defaultParamsToRedact;
    private readonly string[] _excludePathStartsWith;

    public HttpUrlRedactionProcessor(
        IOptions<HttpTracingOptions> options,
        IHttpRouteFormatter routeFormatter,
        IHttpRouteParser routeParser,
        IIncomingHttpRouteUtility routeUtility,
        ILogger<HttpUrlRedactionProcessor> logger)
    {
        _options = Throw.IfMemberNull(options, options.Value);
        _logger = logger;

        _routeFormatter = routeFormatter;
        _routeParser = routeParser;
        _routeUtility = routeUtility;

        _defaultParamsToRedact = _options.RouteParameterDataClasses.ToFrozenDictionary(StringComparer.Ordinal, optimizeForReading: true);
        _excludePathStartsWith = _options.ExcludePathStartsWith.ToArray();

        _logger.ConfiguredHttpTracingOptions(_options);
    }

    public void ProcessRequest(Activity activity, HttpRequest request)
    {
        // remove attributes that might contain sensitive information.
        _ = activity.SetTag(Constants.AttributeUserAgent, null);
        _ = activity.SetTag(Constants.AttributeHttpPath, null);
        _ = activity.SetTag(Constants.AttributeHttpTarget, null);
        _ = activity.SetTag(Constants.AttributeNetHostName, null);
        _ = activity.SetTag(Constants.AttributeNetHostPort, null);
        _ = activity.SetTag(Constants.AttributeHttpScheme, null);
        _ = activity.SetTag(Constants.AttributeHttpFlavor, null);
        _ = activity.SetTag(Constants.OtelStatusCode, null);

        _ = activity.SetTag(Constants.AttributeHttpHost, request.Host.Host);

        if (_options.RequestPathParameterRedactionMode == HttpRouteParameterRedactionMode.None)
        {
            _ = activity.DisplayName = request.Path.Value!;
            var formattedUrl = GetFormattedUrl(request.Scheme, request.Host.Value, request.Path.Value!);
            _ = activity.SetTag(Constants.AttributeHttpUrl, formattedUrl);
        }
        else
        {
            _ = activity.SetTag(Constants.AttributeHttpUrl, null);
        }
    }

    public void ProcessResponse(Activity activity, HttpRequest request)
    {
        if (_options.RequestPathParameterRedactionMode == HttpRouteParameterRedactionMode.None)
        {
            return;
        }

        var httpRoute = GetHttpRoute(activity, request);
        if (string.IsNullOrEmpty(httpRoute))
        {
            _logger.HttpRouteNotFound(activity.OperationName);
            _ = activity.SetTag(
                Constants.AttributeHttpUrl,
                _urlCache.GetOrAdd(
                    (request.Scheme, request.Host.Value, Constants.RequestNameUnknown),
                    (_) => GetFormattedUrl(request.Scheme, request.Host.Value, Constants.RequestNameUnknown)));
            activity.DisplayName = Constants.RequestNameUnknown;
            return;
        }

        if (ShouldExcludePath(httpRoute))
        {
            activity.ActivityTraceFlags = ~ActivityTraceFlags.Recorded;
            return;
        }

        _ = activity.SetTag(Constants.AttributeHttpRoute, null);
        activity.DisplayName = httpRoute;

        var routeSegments = _routeParser.ParseRoute(httpRoute);
        var parametersToRedact = _routeUtility.GetSensitiveParameters(httpRoute, request, _defaultParamsToRedact);

        if (_options.IncludePath)
        {
            httpRoute = _routeFormatter.Format(routeSegments, request.Path, _options.RequestPathParameterRedactionMode, parametersToRedact);
        }
        else
        {
            AddRouteParametersAsTags(activity, request.Path, routeSegments, parametersToRedact);
        }

        var formattedUrl = _urlCache.GetOrAdd(
            (request.Scheme, request.Host.Value, httpRoute),
            (_) => GetFormattedUrl(request.Scheme, request.Host.Value, httpRoute));
        _ = activity.SetTag(Constants.AttributeHttpUrl, formattedUrl);
    }

#pragma warning disable S3995 // URI return values should not be strings
    private static string GetFormattedUrl(string scheme, string host, string path)
    {
        if (path.Length > 0 && path[0] == '/')
        {
            return $"{scheme}{Uri.SchemeDelimiter}{host}{path}";
        }
        else
        {
            return $"{scheme}{Uri.SchemeDelimiter}{host}/{path}";
        }
    }
#pragma warning restore S3995 // URI return values should not be strings

    private static string GetHttpRoute(Activity activity, HttpRequest request)
    {
        var httpRoute = (string?)activity.GetTagItem(Constants.AttributeHttpRoute) ?? string.Empty;
        if (string.IsNullOrEmpty(httpRoute))
        {
            httpRoute = request.GetRoute();
        }

        return httpRoute;
    }

    private void AddRouteParametersAsTags(Activity activity, PathString path, ParsedRouteSegments routeSegments, IReadOnlyDictionary<string, DataClassification> parametersToRedact)
    {
        var routeParameters = ArrayPool<HttpRouteParameter>.Shared.Rent(routeSegments.ParameterCount);
        try
        {
            if (_routeParser.TryExtractParameters(path, routeSegments, _options.RequestPathParameterRedactionMode, parametersToRedact, ref routeParameters))
            {
                for (int i = 0; i < routeSegments.ParameterCount; i++)
                {
                    _ = activity.AddTag(routeParameters[i].Name, routeParameters[i].Value);
                }
            }
        }
        finally
        {
            ArrayPool<HttpRouteParameter>.Shared.Return(routeParameters);
        }
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
