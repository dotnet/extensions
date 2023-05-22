// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Internal;

internal sealed class HttpPathRedactor : IHttpPathRedactor
{
    private readonly HttpRouteParameterRedactionMode _parameterRedactionMode;
    private readonly IHttpRouteFormatter _httpRouteFormatter;
    private readonly IHttpRouteParser _httpRouteParser;

    public HttpPathRedactor(
        IOptions<HttpClientTracingOptions> options,
        IHttpRouteFormatter routeFormatter,
        IHttpRouteParser httpRouteParser)
    {
        var opts = Throw.IfNullOrMemberNull(options, options.Value);
        _parameterRedactionMode = opts.RequestPathParameterRedactionMode;
        _httpRouteFormatter = routeFormatter;
        _httpRouteParser = httpRouteParser;
    }

    public string Redact(string routeTemplate, string httpPath, IReadOnlyDictionary<string, DataClassification> parametersToRedact, out int parameterCount)
    {
        parameterCount = 0;
        if (!IsRouteValid(routeTemplate))
        {
            return TelemetryConstants.Redacted;
        }

        var routeSegments = _httpRouteParser.ParseRoute(routeTemplate);
        parameterCount = routeSegments.ParameterCount;
        return _httpRouteFormatter.Format(routeSegments, httpPath, _parameterRedactionMode, parametersToRedact);
    }

    private static bool IsRouteValid(string route)
        => !string.IsNullOrEmpty(route) && route != TelemetryConstants.Unknown;
}
