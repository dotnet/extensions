// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Http.Telemetry.Tracing.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Telemetry.Tracing;

internal sealed class HttpClientRedactionProcessor
{
    private readonly ILogger<HttpClientRedactionProcessor> _logger;
    private readonly IHttpPathRedactor _httpPathRedactor;
    private readonly FrozenDictionary<string, DataClassification> _parametersToRedact;
    private readonly ConcurrentDictionary<string, string> _urlCache = new();
    private readonly IOutgoingRequestContext _requestMetadataContext;
    private readonly IDownstreamDependencyMetadataManager? _downstreamDependencyMetadataManager;
    private readonly HttpClientTracingOptions _options;

    public HttpClientRedactionProcessor(
        IOptions<HttpClientTracingOptions> options,
        IHttpPathRedactor httpPathRedactor,
        IOutgoingRequestContext requestMetadataContext,
        ILogger<HttpClientRedactionProcessor>? logger = null,
        IDownstreamDependencyMetadataManager? downstreamDependencyMetadataManager = null)
    {
        _options = Throw.IfNullOrMemberNull(options, options.Value);
        _logger = logger ?? NullLogger<HttpClientRedactionProcessor>.Instance;

        _httpPathRedactor = httpPathRedactor;
        _requestMetadataContext = requestMetadataContext;
        _downstreamDependencyMetadataManager = downstreamDependencyMetadataManager;

        _parametersToRedact = _options.RouteParameterDataClasses.ToFrozenDictionary(StringComparer.Ordinal, optimizeForReading: true);

        _logger.ConfiguredHttpClientTracingOptions(_options);
    }

    public void Process(Activity activity, HttpRequestMessage request)
    {
        // Remove tags that shouldn't be exported as they may contain sensitive information.
        _ = activity.SetTag(Constants.AttributeUserAgent, null);
        _ = activity.SetTag(Constants.AttributeHttpTarget, null);
        _ = activity.SetTag(Constants.AttributeHttpPath, null);
        _ = activity.SetTag(Constants.AttributeHttpScheme, null);
        _ = activity.SetTag(Constants.AttributeHttpFlavor, null);
        _ = activity.SetTag(Constants.AttributeNetPeerName, null);
        _ = activity.SetTag(Constants.AttributeNetPeerPort, null);

        if (request.RequestUri == null)
        {
            HttpTracingEventSource.Instance.HttpRequestUriWasNotSet(activity.OperationName, activity.Id);
            _logger.HttpRequestUriWasNotSet(activity.OperationName, activity.Id);
            return;
        }

        _ = activity.SetTag(Constants.AttributeHttpHost, request.RequestUri.Host);
        if (_options.RequestPathParameterRedactionMode == HttpRouteParameterRedactionMode.None)
        {
            var path = request.RequestUri.AbsolutePath;
            _ = activity.DisplayName = path;
            _ = activity.SetTag(Constants.AttributeHttpRoute, path);
            _ = activity.SetTag(Constants.AttributeHttpUrl, GetFormattedUrl(request.RequestUri, path));
            return;
        }

        var httpPath = request.RequestUri.AbsolutePath;
        var requestMetadata = request.GetRequestMetadata() ??
            _requestMetadataContext.RequestMetadata ??
            _downstreamDependencyMetadataManager?.GetRequestMetadata(request);

        if (requestMetadata == null)
        {
            _logger.RequestMetadataIsNotSetForTheRequest(request.RequestUri.AbsoluteUri);

            _ = activity.DisplayName = TelemetryConstants.Unknown;
            _ = activity.SetTag(Constants.AttributeHttpRoute, TelemetryConstants.Unknown);
            _ = activity.SetTag(Constants.AttributeHttpUrl, GetFormattedUrl(request.RequestUri, TelemetryConstants.Unknown));
            return;
        }

        var requestRoute = requestMetadata.RequestRoute;
        if (requestRoute == TelemetryConstants.Unknown)
        {
            _ = activity.DisplayName = requestMetadata.RequestName;
            _ = activity.SetTag(Constants.AttributeHttpRoute, requestMetadata.RequestName);
            _ = activity.SetTag(Constants.AttributeHttpUrl, GetFormattedUrl(request.RequestUri, requestMetadata.RequestName));
        }
        else
        {
            var redactedPath = _httpPathRedactor.Redact(requestRoute, httpPath, _parametersToRedact, out var routeParameterCount);

            string redactedUrl;
            if (routeParameterCount == 0)
            {
                // Route is either empty or has no parameters.
                redactedUrl = _urlCache.GetOrAdd(requestRoute, (_) => GetFormattedUrl(request.RequestUri, redactedPath));
            }
            else
            {
                redactedUrl = GetFormattedUrl(request.RequestUri, redactedPath);
            }

            activity.DisplayName = requestMetadata.RequestName == TelemetryConstants.Unknown
                ? redactedPath : requestMetadata.RequestName;

            _ = activity.SetTag(Constants.AttributeHttpRoute, requestRoute);
            _ = activity.SetTag(Constants.AttributeHttpUrl, redactedUrl);
        }
    }

#pragma warning disable S3995 // URI return values should not be strings
    private static string GetFormattedUrl(Uri requestUri, string path)
    {
        if (path.Length > 0 && path[0] == '/')
        {
            return $"{requestUri.Scheme}{Uri.SchemeDelimiter}{requestUri.Authority}{path}";
        }
        else
        {
            return $"{requestUri.Scheme}{Uri.SchemeDelimiter}{requestUri.Authority}/{path}";
        }
    }
#pragma warning restore S3995 // URI return values should not be strings
}
