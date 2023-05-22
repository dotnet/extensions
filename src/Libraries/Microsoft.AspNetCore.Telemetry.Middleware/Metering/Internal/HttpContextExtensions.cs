// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Telemetry.Internal;

/// <summary>
/// Extensions for <see cref="HttpContext"/>.
/// </summary>
internal static class HttpContextExtensions
{
    /// <summary>
    /// Gets a route template from <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="context">HTTP context of a given request.</param>
    /// <returns>Raw route template string.</returns>
    public static string? GetRouteTemplate(this HttpContext context)
    {
        var endpoint = context.GetEndpoint() as RouteEndpoint;
        return endpoint?.RoutePattern.RawText;
    }
}
