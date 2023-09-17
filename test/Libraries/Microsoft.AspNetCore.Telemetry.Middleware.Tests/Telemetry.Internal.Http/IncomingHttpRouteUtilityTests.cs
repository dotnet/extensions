// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP3_1_OR_GREATER
using System;
#endif
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
#if NETCOREAPP3_1_OR_GREATER
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
#endif
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Internal.Test;

public class IncomingHttpRouteUtilityTests
{
#if NETCOREAPP3_1_OR_GREATER
    [Fact]
    public void GetSensitiveParameter_OneParameterWithDataClassAttrib_ReturnsSensitiveParameter()
    {
        ControllerActionDescriptor controllerActionDescriptor = new ControllerActionDescriptor();
        var parametersInfo = typeof(TestController).GetMethod(nameof(TestController.GetTest1Async))!.GetParameters();

        controllerActionDescriptor.Parameters = new List<ParameterDescriptor>(parametersInfo.Length);
        foreach (var parameterInfo in parametersInfo)
        {
            var parameter = new ControllerParameterDescriptor
            {
                ParameterInfo = parameterInfo,
            };
            controllerActionDescriptor.Parameters.Add(parameter);
        }

        var metadata = new List<object> { controllerActionDescriptor };

        var httpRoute = "/v1/profile/users/userId123";
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

        var routeUtility = new IncomingHttpRouteUtility();
        var sensitiveParameters = routeUtility.GetSensitiveParameters(httpRoute, mockHttpRequest.Object, new Dictionary<string, DataClassification>(StringComparer.Ordinal));
        Assert.Single(sensitiveParameters);
        Assert.True(sensitiveParameters.ContainsKey("userId"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("userId"));
    }

    [Fact]
    public void GetSensitiveParameter_MoreThanOneParameterWithDataClassAttrib_ReturnsAllSensitiveParameters()
    {
        ControllerActionDescriptor controllerActionDescriptor = new ControllerActionDescriptor();
        var parametersInfo = typeof(TestController).GetMethod(nameof(TestController.GetTest2Async))!.GetParameters();

        controllerActionDescriptor.Parameters = new List<ParameterDescriptor>(parametersInfo.Length);
        foreach (var parameterInfo in parametersInfo)
        {
            var parameter = new ControllerParameterDescriptor
            {
                ParameterInfo = parameterInfo,
            };
            controllerActionDescriptor.Parameters.Add(parameter);
        }

        var metadata = new List<object> { controllerActionDescriptor };

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

        var routeUtility = new IncomingHttpRouteUtility();
        var sensitiveParameters = routeUtility.GetSensitiveParameters(httpRoute, mockHttpRequest.Object, new Dictionary<string, DataClassification>(StringComparer.Ordinal));
        Assert.Equal(2, sensitiveParameters.Count);
        Assert.True(sensitiveParameters.ContainsKey("userId"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("userId"));
        Assert.True(sensitiveParameters.ContainsKey("teamId"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("teamId"));
    }

    [Fact]
    public void GetSensitiveParameter_MixParamsWithAndWithoutDataClass_ReturnsOnlySensitiveParameters()
    {
        ControllerActionDescriptor controllerActionDescriptor = new ControllerActionDescriptor();
        var parametersInfo = typeof(TestController).GetMethod(nameof(TestController.GetTest3Async))!.GetParameters();

        controllerActionDescriptor.Parameters = new List<ParameterDescriptor>(parametersInfo.Length);
        foreach (var parameterInfo in parametersInfo)
        {
            var parameter = new ControllerParameterDescriptor
            {
                ParameterInfo = parameterInfo,
            };
            controllerActionDescriptor.Parameters.Add(parameter);
        }

        var metadata = new List<object> { controllerActionDescriptor };

        var httpRoute = "v1/profile/users/{userId}/teams/{teamId}/chats/{chatId}";
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

        var routeUtility = new IncomingHttpRouteUtility();
        var sensitiveParameters = routeUtility.GetSensitiveParameters(httpRoute, mockHttpRequest.Object, new Dictionary<string, DataClassification>(StringComparer.Ordinal));
        Assert.Equal(2, sensitiveParameters.Count);
        Assert.True(sensitiveParameters.ContainsKey("userId"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("userId"));
        Assert.True(sensitiveParameters.ContainsKey("teamId"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("teamId"));

        Assert.False(sensitiveParameters.ContainsKey("chatId"));
    }

    [Fact]
    public void GetSensitiveParameter_NoParameters_ReturnsDefault()
    {
        ControllerActionDescriptor controllerActionDescriptor = new ControllerActionDescriptor();
        var parametersInfo = typeof(TestController).GetMethod(nameof(TestController.GetTest4Async))!.GetParameters();

        controllerActionDescriptor.Parameters = new List<ParameterDescriptor>(parametersInfo.Length);
        foreach (var parameterInfo in parametersInfo)
        {
            var parameter = new ControllerParameterDescriptor
            {
                ParameterInfo = parameterInfo,
            };
            controllerActionDescriptor.Parameters.Add(parameter);
        }

        var metadata = new List<object> { controllerActionDescriptor };

        var httpRoute = "v1/profile/users";
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

        var routeUtility = new IncomingHttpRouteUtility();
        var d = new Dictionary<string, DataClassification>
        {
            { "testKey", FakeClassifications.PrivateData }
        };
        var sensitiveParameters = routeUtility.GetSensitiveParameters(httpRoute, mockHttpRequest.Object, d);
        Assert.Single(sensitiveParameters);
        Assert.True(sensitiveParameters.ContainsKey("testKey"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("testKey"));
    }

    [Fact]
    public void GetSensitiveParameter_AllParametersWithoutDataClass_ReturnsDefault()
    {
        ControllerActionDescriptor controllerActionDescriptor = new ControllerActionDescriptor();
        var parametersInfo = typeof(TestController).GetMethod(nameof(TestController.GetTest1Async))!.GetParameters();

        controllerActionDescriptor.Parameters = new List<ParameterDescriptor>(parametersInfo.Length);
        foreach (var parameterInfo in parametersInfo)
        {
            var parameter = new ControllerParameterDescriptor
            {
                ParameterInfo = parameterInfo,
            };
            controllerActionDescriptor.Parameters.Add(parameter);
        }

        var metadata = new List<object> { controllerActionDescriptor };

        var httpRoute = string.Empty;
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

        var routeUtility = new IncomingHttpRouteUtility();

        var d = new Dictionary<string, DataClassification>
        {
            { "testKey", FakeClassifications.PrivateData }
        };
        var sensitiveParameters = routeUtility.GetSensitiveParameters(httpRoute, mockHttpRequest.Object, d);
        Assert.Single(sensitiveParameters);
        Assert.True(sensitiveParameters.ContainsKey("testKey"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("testKey"));

        Assert.False(sensitiveParameters.ContainsKey("userId"));
        Assert.False(sensitiveParameters.ContainsKey("teamId"));
        Assert.False(sensitiveParameters.ContainsKey("chatId"));
    }

    [Fact]
    public void GetSensitiveParameter_EmptyRoute_ReturnsDefault()
    {
        ControllerActionDescriptor controllerActionDescriptor = new ControllerActionDescriptor();
        var parametersInfo = typeof(TestController).GetMethod(nameof(TestController.GetTest5Async))!.GetParameters();

        controllerActionDescriptor.Parameters = new List<ParameterDescriptor>(parametersInfo.Length);
        foreach (var parameterInfo in parametersInfo)
        {
            var parameter = new ControllerParameterDescriptor
            {
                ParameterInfo = parameterInfo,
            };
            controllerActionDescriptor.Parameters.Add(parameter);
        }

        var metadata = new List<object> { controllerActionDescriptor };

        var httpRoute = "v1/profile/users/{userId}/teams/{teamId}/chats/{chatId}";
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

        var routeUtility = new IncomingHttpRouteUtility();

        var sensitiveParameters = routeUtility.GetSensitiveParameters(httpRoute, mockHttpRequest.Object, new Dictionary<string, DataClassification>(StringComparer.Ordinal));
        Assert.Empty(sensitiveParameters);

        Assert.False(sensitiveParameters.ContainsKey("userId"));
        Assert.False(sensitiveParameters.ContainsKey("teamId"));
        Assert.False(sensitiveParameters.ContainsKey("chatId"));
    }

    [Fact]
    public void GetSensitiveParameter_ParameterWithDataClass_NonEmptyDefault_ReturnsCombinedWithNonDuplicateEntries()
    {
        ControllerActionDescriptor controllerActionDescriptor = new ControllerActionDescriptor();
        var parametersInfo = typeof(TestController).GetMethod(nameof(TestController.GetTest2Async))!.GetParameters();

        controllerActionDescriptor.Parameters = new List<ParameterDescriptor>(parametersInfo.Length);
        foreach (var parameterInfo in parametersInfo)
        {
            var parameter = new ControllerParameterDescriptor
            {
                ParameterInfo = parameterInfo,
            };
            controllerActionDescriptor.Parameters.Add(parameter);
        }

        var metadata = new List<object> { controllerActionDescriptor };

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

        var d = new Dictionary<string, DataClassification>
        {
            { "testKey", FakeClassifications.PrivateData },
            { "teamId", FakeClassifications.PrivateData }
        };

        var routeUtility = new IncomingHttpRouteUtility();
        var sensitiveParameters = routeUtility.GetSensitiveParameters(httpRoute, mockHttpRequest.Object, d);
        Assert.Equal(3, sensitiveParameters.Count);
        Assert.True(sensitiveParameters.ContainsKey("userId"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("userId"));
        Assert.True(sensitiveParameters.ContainsKey("teamId"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("teamId"));
        Assert.True(sensitiveParameters.ContainsKey("testKey"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("testKey"));
    }

    [Fact]
    public void GetSensitiveParameter_ParameterWithDataClass_TakesPrecedenceOverDefaultList()
    {
        ControllerActionDescriptor controllerActionDescriptor = new ControllerActionDescriptor();
        var parametersInfo = typeof(TestController).GetMethod(nameof(TestController.GetTest2Async))!.GetParameters();

        controllerActionDescriptor.Parameters = new List<ParameterDescriptor>(parametersInfo.Length);
        foreach (var parameterInfo in parametersInfo)
        {
            var parameter = new ControllerParameterDescriptor
            {
                ParameterInfo = parameterInfo,
            };
            controllerActionDescriptor.Parameters.Add(parameter);
        }

        var metadata = new List<object> { controllerActionDescriptor };

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

        var d = new Dictionary<string, DataClassification>
        {
            { "userId", FakeClassifications.PublicData }
        };

        var routeUtility = new IncomingHttpRouteUtility();
        var sensitiveParameters = routeUtility.GetSensitiveParameters(httpRoute, mockHttpRequest.Object, d);
        Assert.Equal(2, sensitiveParameters.Count);
        Assert.True(sensitiveParameters.ContainsKey("userId"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("userId"));
        Assert.True(sensitiveParameters.ContainsKey("teamId"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("teamId"));
    }

    [Fact]
    public void GetSensitiveParameter_2ndCallOnwardForSameRouteReturnsCachedResult()
    {
        ControllerActionDescriptor controllerActionDescriptor = new ControllerActionDescriptor();
        var parametersInfo = typeof(TestController).GetMethod(nameof(TestController.GetTest2Async))!.GetParameters();

        controllerActionDescriptor.Parameters = new List<ParameterDescriptor>(parametersInfo.Length);
        foreach (var parameterInfo in parametersInfo)
        {
            var parameter = new ControllerParameterDescriptor
            {
                ParameterInfo = parameterInfo,
            };
            controllerActionDescriptor.Parameters.Add(parameter);
        }

        var metadata = new List<object> { controllerActionDescriptor };

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

        var d = new Dictionary<string, DataClassification>();

        var routeUtility = new IncomingHttpRouteUtility();
        var sensitiveParameters = routeUtility.GetSensitiveParameters(httpRoute, mockHttpRequest.Object, d);
        Assert.Equal(2, sensitiveParameters.Count);
        Assert.True(sensitiveParameters.ContainsKey("userId"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("userId"));
        Assert.True(sensitiveParameters.ContainsKey("teamId"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("teamId"));

        d.Add("testKey", FakeClassifications.PrivateData);
        d.Add("userId", FakeClassifications.PublicData);
        sensitiveParameters = routeUtility.GetSensitiveParameters(httpRoute, mockHttpRequest.Object, d);
        Assert.Equal(2, sensitiveParameters.Count);
        Assert.True(sensitiveParameters.ContainsKey("userId"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("userId"));
        Assert.True(sensitiveParameters.ContainsKey("teamId"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("teamId"));
    }

    [Fact]
    public void GetSensitiveParameter_ControllerActionDescriptorMissing_ReturnsDefault()
    {
        var metadata = new List<object> { };

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

        var routeUtility = new IncomingHttpRouteUtility();
        var d = new Dictionary<string, DataClassification>
        {
            { "testKey", FakeClassifications.PrivateData }
        };
        var sensitiveParameters = routeUtility.GetSensitiveParameters(httpRoute, mockHttpRequest.Object, d);
        Assert.Single(sensitiveParameters);
        Assert.True(sensitiveParameters.ContainsKey("testKey"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("testKey"));
    }

    [Fact]
    public void GetSensitiveParameter_EndpointMissing_ReturnsDefault()
    {
        var metadata = new List<object> { };

        var httpRoute = "v1/profile/users/{userId}/teams/{teamId}";

        Mock<HttpRequest> mockHttpRequest = new Mock<HttpRequest>();
        HttpContext context = new DefaultHttpContext();
        mockHttpRequest.Setup(m => m.HttpContext).Returns(context);

        var routeUtility = new IncomingHttpRouteUtility();
        var d = new Dictionary<string, DataClassification>
        {
            { "testKey", FakeClassifications.PrivateData }
        };
        var sensitiveParameters = routeUtility.GetSensitiveParameters(httpRoute, mockHttpRequest.Object, d);
        Assert.Single(sensitiveParameters);
        Assert.True(sensitiveParameters.ContainsKey("testKey"));
        Assert.Equal(FakeClassifications.PrivateData, sensitiveParameters.GetValueOrDefault("testKey"));
    }
#else
    [Fact]
    public void GetSensitiveParameter_AlwaysReturnsDefault()
    {
        var httpRoute = "v1/profile/users/{userId}/teams/{teamId}";

        Mock<HttpRequest> mockHttpRequest = new Mock<HttpRequest>();
        HttpContext context = new DefaultHttpContext();
        mockHttpRequest.Setup(m => m.HttpContext).Returns(context);

        var routeUtility = new IncomingHttpRouteUtility();
        var d = new Dictionary<string, DataClassification>
        {
            { "testKey", FakeClassifications.PrivateData }
        };
        var sensitiveParameters = routeUtility.GetSensitiveParameters(httpRoute, mockHttpRequest.Object, d);
        Assert.Single(sensitiveParameters);
        Assert.True(sensitiveParameters.ContainsKey("testKey"));
        Assert.True(sensitiveParameters.TryGetValue("testKey", out DataClassification classification));
        Assert.Equal(FakeClassifications.PrivateData, classification);
    }
#endif
}
