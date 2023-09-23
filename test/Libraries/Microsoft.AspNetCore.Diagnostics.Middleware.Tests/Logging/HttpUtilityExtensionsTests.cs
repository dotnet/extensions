// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP3_1_OR_GREATER
using System;
#endif
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
#if NETCOREAPP3_1_OR_GREATER
using Microsoft.AspNetCore.Routing.Patterns;
#endif
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
#if !NETCOREAPP3_1_OR_GREATER
using Opt = Microsoft.Extensions.Options.Options;
#endif

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test;

public class HttpUtilityExtensionsTests
{
    [Fact]
    public void AddHttpRouteUtilities_AddsIncomingHttpRouteUtility()
    {
        var sp = new ServiceCollection().AddHttpRouteUtilities().BuildServiceProvider();

        var routeUtilities = sp.GetService<IIncomingHttpRouteUtility>();
        Assert.NotNull(routeUtilities);
    }

#if NETCOREAPP3_1_OR_GREATER
    [Fact]
    public void GetRoute_ReturnsRouteWhenExists()
    {
        var metadata = new List<object>(1);

        var httpRoute = "v1/profile/users/{userId}/teams/{teamId}";
        var endpoint = new RouteEndpoint(
                c => throw new InvalidOperationException("Test"),
                RoutePatternFactory.Parse(httpRoute),
                0,
                new EndpointMetadataCollection(metadata),
                "Endpoint display name");

        Mock<HttpRequest> mockHttpRequest = new Mock<HttpRequest>();
        HttpContext context = new DefaultHttpContext();
        mockHttpRequest.Setup(m => m.HttpContext).Returns(context);
        context.SetEndpoint(endpoint);

        Assert.Equal(httpRoute, mockHttpRequest.Object.GetRoute());
    }

    [Fact]
    public void GetRoute_NullEndpoint_ReturnsEmpty()
    {
        Mock<HttpRequest> mockHttpRequest = new Mock<HttpRequest>();
        HttpContext context = new DefaultHttpContext();
        mockHttpRequest.Setup(m => m.HttpContext).Returns(context);

        Assert.Equal(string.Empty, mockHttpRequest.Object.GetRoute());
    }

    [Fact]
    public void GetRoute_NullRawText_ReturnsEmpty()
    {
        var metadata = new List<object>(1);
        var endpoint = new RouteEndpoint(
                c => throw new InvalidOperationException("Test"),
                RoutePatternFactory.Pattern(),
                0,
                new EndpointMetadataCollection(metadata),
                "Endpoint display name");

        Mock<HttpRequest> mockHttpRequest = new Mock<HttpRequest>();
        HttpContext context = new DefaultHttpContext();
        mockHttpRequest.Setup(m => m.HttpContext).Returns(context);
        context.SetEndpoint(endpoint);

        Assert.Equal(string.Empty, mockHttpRequest.Object.GetRoute());
    }
#else
    [Fact]
    public void GetRoute_NullRouteData_ReturnsEmpty()
    {
        Mock<HttpRequest> mockHttpRequest = new Mock<HttpRequest>();
        HttpContext context = new DefaultHttpContext();
        mockHttpRequest.SetupGet(x => x.HttpContext).Returns(context);
        Assert.Equal(string.Empty, mockHttpRequest.Object.GetRoute());
    }

    [Fact]
    public void GetRoute_WhenRouteExists_ShouldReturnCorrectRoute()
    {
        Mock<HttpRequest> mockHttpRequest = new Mock<HttpRequest>();

        var httpRoute = "v1/profile/users/{userId}/teams/{teamId}";
        var routeValues = new RouteValueDictionary
        {
            { "route", httpRoute }
        };

        var routeData = new RouteData(routeValues);
        Mock<IRouter> mockRouter = new Mock<IRouter>();

        var routerColl = new RouteCollection();
#pragma warning disable CS0618 // Type or member is obsolete
        routerColl.Add(new Route(
                mockRouter.Object,
                httpRoute,
                new RouteValueDictionary(),
                new Dictionary<string, object>(),
                new RouteValueDictionary(),
                new DefaultInlineConstraintResolver(Opt.Create(new RouteOptions()))));
#pragma warning restore CS0618 // Type or member is obsolete

        routeData.Routers.Add(routerColl);

        var context = new DefaultHttpContext();

        IRoutingFeature routingFeature = new RoutingFeature
        {
            RouteData = routeData
        };
        context.Features.Set(routingFeature);
        mockHttpRequest.SetupGet(x => x.HttpContext).Returns(context);

        Assert.Equal(httpRoute, mockHttpRequest.Object.GetRoute());
    }
#endif
}
