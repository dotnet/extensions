// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETCOREAPP3_1_OR_GREATER
using System.Linq;
#endif
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

/// <summary>
/// Telemetry utility extensions for incoming http requests.
/// </summary>
internal static class HttpUtilityExtensions
{
    /// <summary>
    /// Adds an implementation instance of <see cref="IIncomingHttpRouteUtility"/> to the service collection.
    /// </summary>
    /// <param name="services">Instance of <see cref="IServiceCollection"/>.</param>
    /// <returns>return <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddHttpRouteUtilities(this IServiceCollection services)
    {
        services.TryAddActivatedSingleton<IIncomingHttpRouteUtility, IncomingHttpRouteUtility>();
        return services;
    }

#if NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// Gets the request route for the http request.
    /// </summary>
    /// <param name="request"><see cref="HttpRequest"/> object.</param>
    /// <returns>Returns request route.</returns>
    public static string GetRoute(this HttpRequest request)
    {
        _ = Throw.IfNull(request);

        var endpoint = request.HttpContext.GetEndpoint();
        if (endpoint is RouteEndpoint routeEndpoint)
        {
            return routeEndpoint.RoutePattern.RawText ?? string.Empty;
        }

        return string.Empty;
    }
#else
    /// <summary>
    /// Gets the request route for the http request.
    /// </summary>
    /// <param name="request"><see cref="HttpRequest"/> object.</param>
    /// <returns>Returns request route.</returns>
    public static string GetRoute(this HttpRequest request)
    {
        _ = Throw.IfNull(request);

        var routeData = request.HttpContext.GetRouteData();

        var routes = routeData?.Routers.OfType<RouteCollection>().FirstOrDefault();
        var route = string.Empty;
        if (routes?.Count > 0)
        {
            route = routes[0].ToString();
        }

        return route;
    }
#endif
}
