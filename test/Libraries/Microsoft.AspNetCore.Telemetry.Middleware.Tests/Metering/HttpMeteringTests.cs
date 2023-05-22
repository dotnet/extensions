// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Telemetry.Internal;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Extensions.Telemetry.Testing.Metering;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Test;

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
public class HttpMeteringTests
{
    private const long DefaultClockAdvanceMs = 200;

    private static readonly RequestDelegate _stubRequestDelegate =
        static _ => Task.CompletedTask;

    private readonly FakeTimeProvider _fakeTimeProvider = new();

    private readonly RequestDelegate _advanceTimeRequestDelegate;

    public HttpMeteringTests()
    {
        _advanceTimeRequestDelegate = _ =>
        {
            _fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(DefaultClockAdvanceMs));
            return Task.CompletedTask;
        };
    }

    [Fact]
    public async Task CanLogIncomingRequestMetric()
    {
        using var metricCollector = new MetricCollector(new List<string> { typeof(HttpMeteringMiddleware).FullName! });
        using var host = await FakeHost.CreateBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddRouting()
                    .AddHttpMetering())
                .Configure(app => app
                    .UseRouting()
                    .UseHttpMetering()
                    .UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/some/route/{routeId}", async context =>
                        {
                            context.Response.StatusCode = 401;
                            await context.Response.WriteAsync("TestCompleted");
                        });
                    })))
            .StartAsync();

        using var client = host.GetTestClient();
        using var response = client.GetAsync("/some/route/123").Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.IncomingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("localhost", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("GET /some/route/{routeId}", latest.GetDimension(Metric.ReqName));
        Assert.Equal("401", latest.GetDimension(Metric.RspResultCode));
        Assert.Equal("no_exception", latest.GetDimension(Metric.ExceptionType));

        await host.StopAsync();
    }

    [Fact]
    public async Task CanLogIncomingRequestMetricWithEnricher()
    {
        IIncomingRequestMetricEnricher testEnricher = new TestEnricher(2, "2");

        using var metricCollector = new MetricCollector(new List<string> { typeof(HttpMeteringMiddleware).FullName! });
        using var host = await FakeHost.CreateBuilder()
            .ConfigureWebHost(webBuilder =>
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services
                            .AddRouting()
                            .AddHttpMetering(builder =>
                            {
                                builder.AddMetricEnricher<TestEnricher>();
                                builder.AddMetricEnricher(testEnricher);
                            });
                    })
                    .Configure(app =>
                        app.UseRouting()
                            .UseHttpMetering()
                            .UseEndpoints(endpoints =>
                            {
                                endpoints.MapGet("/some/route/{routeId}", async context =>
                                {
                                    context.Response.StatusCode = 401;
                                    await context.Response.WriteAsync("TestCompleted");
                                });
                            })))
            .StartAsync();

        using var client = host.GetTestClient();
        using var response = await client.GetAsync("/some/route/123").ConfigureAwait(false);

        var latest = metricCollector.GetHistogramValues<long>(Metric.IncomingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("localhost", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("GET /some/route/{routeId}", latest.GetDimension(Metric.ReqName));
        Assert.Equal("401", latest.GetDimension(Metric.RspResultCode));
        Assert.Equal("no_exception", latest.GetDimension(Metric.ExceptionType));
        Assert.Equal("test_value_1", latest.GetDimension("test_property_1"));
        Assert.Equal("test_value_21", latest.GetDimension("test_property_21"));
        Assert.Equal("test_value_22", latest.GetDimension("test_property_22"));

        await host.StopAsync();
    }

    [Fact]
    public async Task CanLogIncomingRequestMetric_UseRoutingAfterMiddleware()
    {
        using var meter = new Meter<HttpMeteringMiddleware>();
        using var metricCollector = new MetricCollector(new List<string> { typeof(HttpMeteringMiddleware).FullName! });
        using var host = await FakeHost.CreateBuilder()
            .ConfigureWebHost(webBuilder =>
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services
                            .AddHttpMetering();
                        services.AddRouting();
                    })
                    .Configure(app =>
                        app.UseHttpMetering()
                            .UseRouting()
                            .UseEndpoints(endpoints =>
                            {
                                endpoints.MapGet("/some/route/{routeId}", async context =>
                                {
                                    context.Response.StatusCode = 503;
                                    await context.Response.WriteAsync("TestCompleted");
                                });
                            })))
            .StartAsync();

        using var client = host.GetTestClient();
        using var response = await client.GetAsync("/some/route/456").ConfigureAwait(false);

        var latest = metricCollector.GetHistogramValues<long>(Metric.IncomingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("localhost", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("GET /some/route/{routeId}", latest.GetDimension(Metric.ReqName));
        Assert.Equal("503", latest.GetDimension(Metric.RspResultCode));
        Assert.Equal("no_exception", latest.GetDimension(Metric.ExceptionType));

        await host.StopAsync();
    }

    [Fact]
    public async Task CanLogIncomingRequestMetric_TimeoutException()
    {
        using var meter = new Meter<HttpMeteringMiddleware>();
        using var metricCollector = new MetricCollector(new List<string> { typeof(HttpMeteringMiddleware).FullName! });
        using var host = await FakeHost.CreateBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddHttpMetering()
                    .AddRouting())
                .Configure(app => app
                    .UseHttpMetering()
                    .UseRouting()
                    .UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/some/route/{routeId}", async context =>
                        {
                            context.Response.StatusCode = 301;
                            await context.Response.WriteAsync("TestCompleted");
                            throw new TimeoutException();
                        });

                        endpoints.MapGet("/some/route2/{routeId}", async context =>
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync("TestCompleted");
                            throw new TimeoutException();
                        });

                        endpoints.MapGet("/some/route3/{routeId}", async context =>
                        {
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync("TestCompleted");
                            throw new TimeoutException();
                        });
                    })))
            .StartAsync();

        string exceptionTypeName = typeof(TimeoutException).FullName!;

        using var client = host.GetTestClient();

        await Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetAsync("/some/route/456"));

        var latest = metricCollector.GetHistogramValues<long>(Metric.IncomingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("localhost", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("GET /some/route/{routeId}", latest.GetDimension(Metric.ReqName));
        Assert.Equal("500", latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(exceptionTypeName, latest.GetDimension(Metric.ExceptionType));
        metricCollector.Clear();

        await Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetAsync("/some/route2/456"));
        latest = metricCollector.GetHistogramValues<long>(Metric.IncomingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("localhost", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("GET /some/route2/{routeId}", latest.GetDimension(Metric.ReqName));
        Assert.Equal("400", latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(exceptionTypeName, latest.GetDimension(Metric.ExceptionType));
        metricCollector.Clear();

        await Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetAsync("/some/route3/456"));
        latest = metricCollector.GetHistogramValues<long>(Metric.IncomingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("localhost", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("GET /some/route3/{routeId}", latest.GetDimension(Metric.ReqName));
        Assert.Equal("500", latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(exceptionTypeName, latest.GetDimension(Metric.ExceptionType));
        metricCollector.Clear();

        await host.StopAsync();
    }

    [Fact]
    public void InvokeAsync_HttpMeteringMiddleware_Success()
    {
        string httpMethod = HttpMethods.Get;
        string hostString = "teams.microsoft.com";
        string route = "/tenant/{tenantid}/users/{userId}";

        using var meter = new Meter<HttpMeteringMiddleware>();
        using var metricCollector = new MetricCollector(new List<string> { typeof(HttpMeteringMiddleware).FullName! });
        var middleware = SetupMockMiddleware(meter: meter, new List<IIncomingRequestMetricEnricher>());

        var context = new DefaultHttpContext();
        context.Request.Method = httpMethod;
        context.Request.Host = new HostString(hostString);
        var routePattern = RoutePatternFactory.Parse(route);
        context.SetEndpoint(new RouteEndpoint(
            _stubRequestDelegate, routePattern, 0, EndpointMetadataCollection.Empty, string.Empty));

        middleware.InvokeAsync(context, _advanceTimeRequestDelegate).Wait();

        var latest = metricCollector.GetHistogramValues<long>(Metric.IncomingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal(hostString, latest.GetDimension(Metric.ReqHost));
        Assert.Equal("GET " + route, latest.GetDimension(Metric.ReqName));
        Assert.Equal("200", latest.GetDimension(Metric.RspResultCode));
        Assert.Equal("no_exception", latest.GetDimension(Metric.ExceptionType));
    }

    [Fact]
    public void PerfStopwatch_ReturnsTotalMilliseconds_InsteadOfFraction()
    {
        const long TimeAdvanceMs = 1500L; // We need to use any value greater than 1000 (1 second)

        string httpMethod = HttpMethods.Get;
        string hostString = "teams.microsoft.com";
        string route = "/tenant/{tenantid}/users/{userId}";

        using var meter = new Meter<HttpMeteringMiddleware>();
        using var metricCollector = new MetricCollector(new List<string> { typeof(HttpMeteringMiddleware).FullName! });
        var middleware = SetupMockMiddleware(meter: meter, new List<IIncomingRequestMetricEnricher>());

        var context = new DefaultHttpContext();
        context.Request.Method = httpMethod;
        context.Request.Host = new HostString(hostString);
        var routePattern = RoutePatternFactory.Parse(route);
        context.SetEndpoint(new RouteEndpoint(
            _stubRequestDelegate, routePattern, 0, EndpointMetadataCollection.Empty, string.Empty));

        RequestDelegate next = _ =>
        {
            _fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(TimeAdvanceMs));
            return Task.CompletedTask;
        };

        middleware.InvokeAsync(context, next).Wait();

        var latest = metricCollector.GetHistogramValues<long>(Metric.IncomingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal(hostString, latest.GetDimension(Metric.ReqHost));
        Assert.Equal("GET " + route, latest.GetDimension(Metric.ReqName));
        Assert.Equal("200", latest.GetDimension(Metric.RspResultCode));
        Assert.Equal("no_exception", latest.GetDimension(Metric.ExceptionType));
    }

    [Fact]
    public void InvokeAsync_HttpMeteringMiddleware_NullRoute()
    {
        string httpMethod = HttpMethods.Post;
        string hostString = "teams.microsoft.com";

        using var meter = new Meter<HttpMeteringMiddleware>();
        using var metricCollector = new MetricCollector(new List<string> { typeof(HttpMeteringMiddleware).FullName! });
        var middleware = SetupMockMiddleware(meter: meter, new List<IIncomingRequestMetricEnricher>());

        var context = new DefaultHttpContext();
        context.Request.Method = httpMethod;
        context.Request.Host = new HostString(hostString);
        context.Response.StatusCode = 409;

        middleware.InvokeAsync(context, _advanceTimeRequestDelegate).Wait();

        var latest = metricCollector.GetHistogramValues<long>(Metric.IncomingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal(hostString, latest.GetDimension(Metric.ReqHost));
        Assert.Equal("POST unsupported_route", latest.GetDimension(Metric.ReqName));
        Assert.Equal("409", latest.GetDimension(Metric.RspResultCode));
        Assert.Equal("no_exception", latest.GetDimension(Metric.ExceptionType));
    }

    [Fact]
    public void InvokeAsync_HttpMeteringMiddleware_NullHostString()
    {
        string httpMethod = HttpMethods.Post;
        string expectedHostString = "unknown_host_name";

        using var meter = new Meter<HttpMeteringMiddleware>();
        using var metricCollector = new MetricCollector(new List<string> { typeof(HttpMeteringMiddleware).FullName! });
        var middleware = SetupMockMiddleware(meter: meter, new List<IIncomingRequestMetricEnricher>());

        var context = new DefaultHttpContext();
        context.Request.Method = httpMethod;
        context.Response.StatusCode = 409;

        middleware.InvokeAsync(context, _advanceTimeRequestDelegate).Wait();

        var latest = metricCollector.GetHistogramValues<long>(Metric.IncomingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal(expectedHostString, latest.GetDimension(Metric.ReqHost));
        Assert.Equal("POST unsupported_route", latest.GetDimension(Metric.ReqName));
        Assert.Equal("409", latest.GetDimension(Metric.RspResultCode));
        Assert.Equal("no_exception", latest.GetDimension(Metric.ExceptionType));
    }

    [Fact]
    public void InvokeAsync_HttpMeteringMiddleware_InternalServerError()
    {
        string httpMethod = HttpMethods.Post;
        string hostString = "teams.microsoft.com";

        using var meter = new Meter<HttpMeteringMiddleware>();
        using var metricCollector = new MetricCollector(new List<string> { typeof(HttpMeteringMiddleware).FullName! });
        var middleware = SetupMockMiddleware(meter: meter, new List<IIncomingRequestMetricEnricher>());

        var context = new DefaultHttpContext();
        context.Request.Method = httpMethod;
        context.Request.Host = new HostString(hostString);

        static Task next(HttpContext context) => throw new InvalidOperationException();

        Assert.Throws<InvalidOperationException>(() => middleware.InvokeAsync(context, next).RunSynchronously());

        var latest = metricCollector.GetHistogramValues<long>(Metric.IncomingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal(hostString, latest.GetDimension(Metric.ReqHost));
        Assert.Equal("POST unsupported_route", latest.GetDimension(Metric.ReqName));
        Assert.Equal("500", latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(typeof(InvalidOperationException).FullName!, latest.GetDimension(Metric.ExceptionType));
    }

    [Fact]
    public void SendAsync_MultiEnrich()
    {
        string hostString = "teams.microsoft.com";

        for (int i = 1; i <= 15; i++)
        {
            using var meter = new Meter<HttpMeteringMiddleware>();
            using var metricCollector = new MetricCollector(new List<string> { typeof(HttpMeteringMiddleware).FullName! });
            var middleware = SetupMockMiddleware(meter: meter, new List<IIncomingRequestMetricEnricher>
                {
                    new TestEnricher(i)
                });

            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Host = new HostString(hostString);
            context.Response.StatusCode = 409;

            middleware.InvokeAsync(context, Mock.Of<RequestDelegate>()).Wait();

            var latest = metricCollector.GetHistogramValues<long>(Metric.IncomingRequestMetricName)!.LatestWritten!;

            Assert.NotNull(latest);
            Assert.Equal(hostString, latest.GetDimension(Metric.ReqHost));
            Assert.Equal("GET unsupported_route", latest.GetDimension(Metric.ReqName));
            Assert.Equal("409", latest.GetDimension(Metric.RspResultCode));
            Assert.Equal("no_exception", latest.GetDimension(Metric.ExceptionType));

            for (int j = 0; j < i; j++)
            {
                Assert.Equal($"test_value_{j + 1}", latest.GetDimension($"test_property_{j + 1}"));
            }
        }
    }

    [Fact]
    public void InvokeAsync_MultipleEnrichers()
    {
        string hostString = "teams.microsoft.com";

        using var meter = new Meter<HttpMeteringMiddleware>();
        using var metricCollector = new MetricCollector(new List<string> { typeof(HttpMeteringMiddleware).FullName! });
        var middleware = SetupMockMiddleware(meter: meter, new List<IIncomingRequestMetricEnricher>
            {
                new TestEnricher(2),
                new TestEnricher(2, "2"),
            });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Host = new HostString(hostString);
        context.Response.StatusCode = 409;

        middleware.InvokeAsync(context, Mock.Of<RequestDelegate>()).Wait();

        var latest = metricCollector.GetHistogramValues<long>(Metric.IncomingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal(hostString, latest.GetDimension(Metric.ReqHost));
        Assert.Equal("GET unsupported_route", latest.GetDimension(Metric.ReqName));
        Assert.Equal("409", latest.GetDimension(Metric.RspResultCode));
        Assert.Equal("no_exception", latest.GetDimension(Metric.ExceptionType));

        Assert.Equal("test_value_1", latest.GetDimension("test_property_1"));
        Assert.Equal("test_value_2", latest.GetDimension("test_property_2"));
        Assert.Equal("test_value_21", latest.GetDimension("test_property_21"));
        Assert.Equal("test_value_22", latest.GetDimension("test_property_22"));
    }

    [Fact]
    public async Task InvokeAsync_HttpMeteringMiddleware_PropertyBagEdgeCase()
    {
        string httpMethod = HttpMethods.Post;
        string hostString = "teams.microsoft.com";

        using var meter = new Meter<HttpMeteringMiddleware>();
        using var metricCollector = new MetricCollector(new List<string> { typeof(HttpMeteringMiddleware).FullName! });
        var middleware = SetupMockMiddleware(meter: meter, new List<IIncomingRequestMetricEnricher>
            {
                new PropertyBagEdgeCaseEnricher()
            });

        var context = new DefaultHttpContext();
        context.Request.Method = httpMethod;
        context.Request.Host = new HostString(hostString);
        context.Response.StatusCode = 409;

        await middleware.InvokeAsync(context, Mock.Of<RequestDelegate>());

        var latest = metricCollector.GetHistogramValues<long>(Metric.IncomingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal(hostString, latest.GetDimension(Metric.ReqHost));
        Assert.Equal("POST unsupported_route", latest.GetDimension(Metric.ReqName));
        Assert.Equal("409", latest.GetDimension(Metric.RspResultCode));
        Assert.Equal("no_exception", latest.GetDimension(Metric.ExceptionType));

        Assert.Equal("test_val", latest.GetDimension("non_null_object_property"));
    }

    [Fact]
    public void HttpMeteringMiddleware_Fail_16DEnrich()
    {
        using var meter = new Meter<HttpMeteringMiddleware>();
        using var metricCollector = new MetricCollector(new List<string> { typeof(HttpMeteringMiddleware).FullName! });
        Assert.Throws<ArgumentOutOfRangeException>(() => SetupMockMiddleware(
            meter: meter,
            new List<IIncomingRequestMetricEnricher>
            {
                    new TestEnricher(16)
            }));
    }

    [Fact]
    public void HttpMeteringMiddleware_Fail_RepeatCustomDimensions()
    {
        using var meter = new Meter<HttpMeteringMiddleware>();
        using var metricCollector = new MetricCollector(new List<string> { typeof(HttpMeteringMiddleware).FullName! });
        Assert.Throws<ArgumentException>(() => SetupMockMiddleware(
            meter: meter,
            new List<IIncomingRequestMetricEnricher>
            {
                    new TestEnricher(),
                    new TestEnricher()
            }));
    }

    [Fact]
    public void HttpMeteringMiddleware_Fail_RepeatDefaultDimensions()
    {
        using var meter = new Meter<HttpMeteringMiddleware>();
        using var metricCollector = new MetricCollector(new List<string> { typeof(HttpMeteringMiddleware).FullName! });
        Assert.Throws<ArgumentException>(() => SetupMockMiddleware(
            meter: meter,
            new List<IIncomingRequestMetricEnricher>
            {
                    new SameDefaultDimEnricher()
            }));
    }

    [Fact]
    public void ServiceCollection_GivenNullArguments_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((HttpMeteringBuilder)null!).AddMetricEnricher<NullRequestEnricher>());

        Assert.Throws<ArgumentNullException>(() =>
            ((HttpMeteringBuilder)null!).AddMetricEnricher(Mock.Of<IIncomingRequestMetricEnricher>()));

        Assert.Throws<ArgumentNullException>(() =>
            new HttpMeteringBuilder(null!)
                .AddMetricEnricher(null!));
    }

    [Fact]
    public void ServiceCollection_AddMultipleRequestEnrichersSuccessfully()
    {
        IIncomingRequestMetricEnricher testEnricher = new TestEnricher();
        var services = new ServiceCollection();
        services.AddSingleton<IIncomingRequestMetricEnricher, NullRequestEnricher>();
        services.AddSingleton(testEnricher);

        using var provider = services.BuildServiceProvider();
        var enrichersCollection = provider.GetServices<IIncomingRequestMetricEnricher>();

        var enricherCount = 0;
        foreach (var enricher in enrichersCollection)
        {
            enricherCount++;
        }

        Assert.Equal(2, enricherCount);
    }

    private HttpMeteringMiddleware SetupMockMiddleware(Meter<HttpMeteringMiddleware> meter, IEnumerable<IIncomingRequestMetricEnricher> requestMetricEnrichers)
    {
        var propertyBagPoolMock = new Mock<ObjectPool<MetricEnrichmentPropertyBag>>();
        propertyBagPoolMock
           .Setup(o => o.Get())
           .Returns(new MetricEnrichmentPropertyBag());

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(meter);
        services.AddHttpMetering();
        foreach (IIncomingRequestMetricEnricher requestMetricEnricher in requestMetricEnrichers)
        {
            services.AddSingleton(requestMetricEnricher);
        }

        using var serviceProvider = services.BuildServiceProvider();

        var middleware = serviceProvider.GetRequiredService<HttpMeteringMiddleware>();
        middleware.TimeProvider = _fakeTimeProvider;
        return middleware;
    }
}
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
