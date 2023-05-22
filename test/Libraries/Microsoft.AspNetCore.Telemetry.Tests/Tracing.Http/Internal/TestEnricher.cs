// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Test.Internal;

public sealed class TestEnricher : IHttpTraceEnricher
{
    public void Enrich(Activity activity, HttpRequest request)
    {
        Assert.NotNull(request);

        var endpoint = request?.HttpContext.Features.Get<IEndpointFeature>()?.Endpoint;
        if (endpoint is RouteEndpoint routeEndpoint)
        {
            activity.AddTag(Constants.AttributeHttpRoute, routeEndpoint.RoutePattern.RawText);
        }
    }
}
