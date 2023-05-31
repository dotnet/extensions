// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Telemetry.Metering.Internal;
using Microsoft.Extensions.Http.Telemetry.Metering.Test.Internal;
using Microsoft.Extensions.Telemetry;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Extensions.Telemetry.Testing.Metering;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Metering.Test;
#pragma warning disable CA2000 // Not necessary to dispose all resources in test class.
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
public sealed class HttpMeteringHandlerTests : IDisposable
{
    private const long DefaultClockAdvanceMs = 200;
    private const string DelayPropertyName = nameof(DelayPropertyName);

    private static readonly Uri _failureUri = new("https://www.example-failure.com/foo?bar");
    private static readonly Uri _successfullUri = new("https://www.example-success.com/foo?bar");

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly FakeTimeProvider _fakeTimeProvider = new();

    private long _clockAdvanceMs = DefaultClockAdvanceMs;

    public HttpMeteringHandlerTests()
    {
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    [Fact]
    public void SendAsync_Success_NoNamesSet()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);

        var client = CreateClientWithHandler(meter);

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, _successfullUri);

        _ = client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token).Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;
        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal(TelemetryConstants.Unknown, latest.GetDimension(Metric.DependencyName));
        Assert.Equal($"PUT {TelemetryConstants.Unknown}", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
    }

    [Fact]
    public void SendAsync_Success()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var client = CreateClientWithHandler(meter);

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _successfullUri);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            DependencyName = "success_service",
            RequestRoute = "/foo"
        });

        _ = client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token).Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;
        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal($"GET /foo", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
    }

    [Fact]
    public void PerfStopwatch_ReturnsTotalMiliseconds_InsteadOfFraction()
    {
        const long TimeAdvanceMs = 1500L; // We need to use any value greater than 1000 (1 second)
        const string ServiceName = "success_service";

        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var client = CreateClientWithHandler(meter);

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _successfullUri);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            DependencyName = ServiceName,
            RequestRoute = "/foo"
        });
        _clockAdvanceMs = TimeAdvanceMs;

        _ = client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token).Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
    }

    [Fact]
    public async Task SendAsync_Exception()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var client = CreateClientWithHandler(meter);

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _failureUri);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            DependencyName = "failure_service",
            RequestRoute = "/foo/failure",
            RequestName = "TestRequestName"
        });

        await Assert.ThrowsAsync<HttpRequestException>(async () => await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token));

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-failure.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("failure_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("POST TestRequestName", latest.GetDimension(Metric.ReqName));
        Assert.Equal(500, latest.GetDimension(Metric.RspResultCode));
    }

    [Fact]
    public void SendAsync_SetReqMetadata_OnAsyncContext_Success()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var requestMetadataContextMock = new Mock<IOutgoingRequestContext>();
        var client = CreateClientWithHandler(meter, requestMetadataContextMock.Object);

        var requestMetadata = new RequestMetadata
        {
            DependencyName = "success_service",
            RequestRoute = "/foo"
        };
        requestMetadataContextMock.Setup(m => m.RequestMetadata).Returns(requestMetadata);

        _ = client.GetAsync(_successfullUri, _cancellationTokenSource.Token).Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
    }

    [Fact]
    public void PerfStopwatch_SetReqMetadata_OnAsyncContext_ReturnsTotalMiliseconds_InsteadOfFraction()
    {
        const long TimeAdvanceMs = 1500L; // We need to use any value greater than 1000 (1 second)
        const string ServiceName = "success_service";

        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var requestMetadataContextMock = new Mock<IOutgoingRequestContext>();
        var client = CreateClientWithHandler(meter, requestMetadataContextMock.Object);

        var requestMetadata = new RequestMetadata
        {
            DependencyName = ServiceName,
            RequestRoute = "/foo"
        };

        requestMetadataContextMock.Setup(m => m.RequestMetadata).Returns(requestMetadata);

        _clockAdvanceMs = TimeAdvanceMs;

        _ = client.GetAsync(_successfullUri, _cancellationTokenSource.Token).Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
    }

    [Fact]
    public async Task SendAsync_SetReqMetadata_OnAsyncContext_Exception()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var requestMetadataContextMock = new Mock<IOutgoingRequestContext>();
        var client = CreateClientWithHandler(meter, requestMetadataContextMock.Object);

        var requestMetadata = new RequestMetadata
        {
            DependencyName = "failure_service",
            RequestRoute = "/foo/failure",
            RequestName = "TestRequestName"
        };
        requestMetadataContextMock.Setup(m => m.RequestMetadata).Returns(requestMetadata);

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _failureUri);
        await Assert.ThrowsAsync<HttpRequestException>(async () => await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token));

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-failure.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("failure_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("POST TestRequestName", latest.GetDimension(Metric.ReqName));
        Assert.Equal(500, latest.GetDimension(Metric.RspResultCode));
    }

    [Fact]
    public void SendAsync_WithDownstreamDependencyMetadata_OnAsyncContext_Success()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var downstreamDependencyMetadataManagerMock = new Mock<IDownstreamDependencyMetadataManager>();
        var client = CreateClientWithHandler(meter, downstreamDependencyMetadataManager: downstreamDependencyMetadataManagerMock.Object);

        var requestMetadata = new RequestMetadata
        {
            DependencyName = "success_service",
            RequestRoute = "/foo"
        };
        downstreamDependencyMetadataManagerMock.Setup(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>())).Returns(requestMetadata);

        _ = client.GetAsync(_successfullUri, _cancellationTokenSource.Token).Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
    }

    [Fact]
    public void PerfStopwatch_WithDownstreamDependencyMetadata_OnAsyncContext_ReturnsTotalMiliseconds_InsteadOfFraction()
    {
        const long TimeAdvanceMs = 1500L; // We need to use any value greater than 1000 (1 second)
        const string ServiceName = "success_service";

        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var downstreamDependencyMetadataManagerMock = new Mock<IDownstreamDependencyMetadataManager>();
        var client = CreateClientWithHandler(meter, downstreamDependencyMetadataManager: downstreamDependencyMetadataManagerMock.Object);

        var requestMetadata = new RequestMetadata
        {
            DependencyName = ServiceName,
            RequestRoute = "/foo"
        };

        downstreamDependencyMetadataManagerMock.Setup(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>())).Returns(requestMetadata);

        _clockAdvanceMs = TimeAdvanceMs;

        _ = client.GetAsync(_successfullUri, _cancellationTokenSource.Token).Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
    }

    [Fact]
    public async Task SendAsync_WithDownstreamDependencyMetadata_OnAsyncContext_Exception()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var dependencyDataManagerMock = new Mock<IDownstreamDependencyMetadataManager>();
        var client = CreateClientWithHandler(meter, downstreamDependencyMetadataManager: dependencyDataManagerMock.Object);

        var requestMetadata = new RequestMetadata
        {
            DependencyName = "failure_service",
            RequestRoute = "/foo/failure",
            RequestName = "TestRequestName"
        };
        dependencyDataManagerMock.Setup(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>())).Returns(requestMetadata);

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _failureUri);
        await Assert.ThrowsAsync<HttpRequestException>(async () => await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token));

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-failure.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("failure_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("POST TestRequestName", latest.GetDimension(Metric.ReqName));
        Assert.Equal(500, latest.GetDimension(Metric.RspResultCode));
    }

    [Fact]
    public void GetHostName_Returns_Unknown_For_Empty_RequestUri()
    {
        var c = HttpMeteringHandler.GetHostName(new HttpRequestMessage());

        Assert.Equal(TelemetryConstants.Unknown, c);
    }

    [Fact]
    public async Task SendAsync_MultiEnrich()
    {
        for (int i = 1; i <= 14; i++)
        {
            using var meter = new Meter<HttpMeteringHandler>();
            using var metricCollector = new MetricCollector(meter);
            var client = CreateClientWithHandler(meter, new List<IOutgoingRequestMetricEnricher>
                {
                    new TestEnricher(i),
                });

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _successfullUri);
            httpRequestMessage.SetRequestMetadata(new RequestMetadata
            {
                DependencyName = "success_service",
                RequestRoute = "/foo"
            });

            _ = await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token);

            var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

            Assert.NotNull(latest);
            Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
            Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
            Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
            Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));

            for (int j = 0; j < i; j++)
            {
                Assert.Equal($"test_value_{j + 1}", latest.GetDimension($"test_property_{j + 1}"));
            }
        }
    }

    [Fact]
    public async Task SendAsync_MultiEnrich_UsingIMeter()
    {
        for (int i = 1; i <= 14; i++)
        {
            using var meter = new Meter<HttpMeteringHandler>();
            using var metricCollector = new MetricCollector(meter);
            var handler = new HttpMeteringHandler(meter, new List<IOutgoingRequestMetricEnricher>
                {
                    new TestEnricher(i),
                })
            {
                InnerHandler = new TestHandlerStub(InnerHandlerFunction)
            };

            var client = new System.Net.Http.HttpClient(handler);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _successfullUri);
            httpRequestMessage.SetRequestMetadata(new RequestMetadata
            {
                DependencyName = "success_service",
                RequestRoute = "/foo"
            });

            _ = await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token);

            var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

            Assert.NotNull(latest);
            Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
            Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
            Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
            Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));

            for (int j = 0; j < i; j++)
            {
                Assert.Equal($"test_value_{j + 1}", latest.GetDimension($"test_property_{j + 1}"));
            }
        }
    }

    [Fact]
    public async Task InvokeAsync_HttpMeteringHandler_MultipleEnrichers()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var client = CreateClientWithHandler(meter, new List<IOutgoingRequestMetricEnricher>
            {
                new TestEnricher(2),
                new TestEnricher(2, "2"),
            });

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _successfullUri);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            DependencyName = "success_service",
            RequestRoute = "/foo"
        });

        _ = await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token);
        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal("test_value_1", latest.GetDimension("test_property_1"));
        Assert.Equal("test_value_2", latest.GetDimension("test_property_2"));
        Assert.Equal("test_value_21", latest.GetDimension("test_property_21"));
        Assert.Equal("test_value_22", latest.GetDimension("test_property_22"));
    }

    [Fact]
    public async Task InvokeAsync_HttpMeteringHandler_PropertyBagEdgeCase()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var client = CreateClientWithHandler(meter, new List<IOutgoingRequestMetricEnricher>
            {
                new PropertyBagEdgeCaseEnricher(),
            });

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _successfullUri);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            DependencyName = "success_service",
            RequestRoute = "/foo"
        });

        _ = await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token);
        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal("test_val", latest.GetDimension("non_null_object_property"));
    }

    [Fact]
    public void HttpMeteringHandler_Fail_16DEnrich()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateClientWithHandler(meter, new List<IOutgoingRequestMetricEnricher>
            {
                    new TestEnricher(16)
            }));
    }

    [Fact]
    public void HttpMeteringHandler_Fail_RepeatCustomDimensions()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        Assert.Throws<ArgumentException>(() =>
            CreateClientWithHandler(meter, new List<IOutgoingRequestMetricEnricher>
            {
                    new TestEnricher(1),
                    new TestEnricher(1),
            }));
    }

    [Fact]
    public void HttpMeteringHandler_Fail_RepeatDefaultDimensions()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        Assert.Throws<ArgumentException>(() =>
            CreateClientWithHandler(meter, new List<IOutgoingRequestMetricEnricher>
            {
                    new SameDefaultDimEnricher(),
            }));
    }

    [Fact]
    public void ServiceCollection_GivenNullArguments_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddOutgoingRequestMetricEnricher<NullOutgoingRequestMetricEnricher>());

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddOutgoingRequestMetricEnricher(Mock.Of<IOutgoingRequestMetricEnricher>()));

        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection()
                .AddOutgoingRequestMetricEnricher(null!));
    }

    [Fact]
    public void ServiceCollection_AddMultipleOutgoingRequestEnrichersSuccessfully()
    {
        IOutgoingRequestMetricEnricher testEnricher = new TestEnricher();
        var services = new ServiceCollection();
        services.AddOutgoingRequestMetricEnricher<NullOutgoingRequestMetricEnricher>();
        services.AddOutgoingRequestMetricEnricher(testEnricher);

        using var provider = services.BuildServiceProvider();
        var enrichersCollection = provider.GetServices<IOutgoingRequestMetricEnricher>();

        var enricherCount = 0;
        foreach (var enricher in enrichersCollection)
        {
            enricherCount++;
        }

        Assert.Equal(2, enricherCount);
    }

    private System.Net.Http.HttpClient CreateClientWithHandler(
        Meter<HttpMeteringHandler> meter,
        IEnumerable<IOutgoingRequestMetricEnricher> outgoingRequestMetricEnrichers)
    {
        var handler = new HttpMeteringHandler(meter, outgoingRequestMetricEnrichers)
        {
            InnerHandler = new TestHandlerStub(InnerHandlerFunction)
        };

        var client = new System.Net.Http.HttpClient(handler);
        return client;
    }

    private System.Net.Http.HttpClient CreateClientWithHandler(
        Meter<HttpMeteringHandler> meter,
        IOutgoingRequestContext? requestMetadataContext = null,
        IDownstreamDependencyMetadataManager? downstreamDependencyMetadataManager = null)
    {
        var handler = new HttpMeteringHandler(meter, Array.Empty<IOutgoingRequestMetricEnricher>(), requestMetadataContext, downstreamDependencyMetadataManager)
        {
            InnerHandler = new TestHandlerStub(InnerHandlerFunction),
            TimeProvider = _fakeTimeProvider
        };

        var client = new System.Net.Http.HttpClient(handler);
        return client;
    }

    private Task<HttpResponseMessage> InnerHandlerFunction(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(_clockAdvanceMs));

        if (request.RequestUri == _failureUri)
        {
            throw new HttpRequestException("Something went wrong");
        }

        var response = new HttpResponseMessage { StatusCode = HttpStatusCode.Created };
        return Task.FromResult(response);
    }

    [Fact]
    public static void AddHttpClientMetering_NullHttpClient()
    {
        IHttpClientBuilder nullBuilder = null!;
        Assert.Throws<ArgumentNullException>(() => nullBuilder.AddHttpClientMetering());
    }

    [Fact]
    public static void AddHttpClientMetering_NullServiceCollection_Throws()
    {
        IServiceCollection services = null!;
        Assert.Throws<ArgumentNullException>(() => services.AddDefaultHttpClientMetering());
    }

    [Fact]
    public static void AddHttpClientMetering_CreatesClientSuccessfully()
    {
        using var sp = new ServiceCollection()
            .RegisterMetering()
            .AddHttpClient()
            .AddDefaultHttpClientMetering()
            .BuildServiceProvider();

        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

        using var httpClient = httpClientFactory?.CreateClient();

        Assert.NotNull(httpClient);
    }

    [Fact]
    public static void AddHttpClientMetering_EnsureBuild()
    {
        const string HttpClientIdentifier = "HttpClientClass";

        using var provider = new ServiceCollection()
            .RegisterMetering()
            .AddHttpClient(HttpClientIdentifier)
            .AddHttpClientMetering()
            .Services
#if NETCOREAPP3_1_OR_GREATER
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
#else
            .BuildServiceProvider(validateScopes: true);
#endif
        var client = provider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(HttpClientIdentifier);

        Assert.NotNull(client);
    }

    [Fact]
    public static void AddHttpClientMetering_Enrichers_EnsureBuild()
    {
        const string HttpClientIdentifier = "HttpClientClass";
        IOutgoingRequestMetricEnricher testEnricher = new TestEnricher(1, "prefxi");

        using var provider = new ServiceCollection()
            .RegisterMetering()
            .AddHttpClient(HttpClientIdentifier)
            .AddHttpClientMetering()
            .Services
            .AddOutgoingRequestMetricEnricher(testEnricher)
            .AddOutgoingRequestMetricEnricher<TestEnricher>()
#if NETCOREAPP3_1_OR_GREATER
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
#else
            .BuildServiceProvider(validateScopes: true);
#endif
        var client = provider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(HttpClientIdentifier);

        Assert.NotNull(client);
    }

    [Fact]
    public static void AddDefaultHttpClientMetering_RequestMetadataSetSuccessfully()
    {
        using var sp = new ServiceCollection()
            .RegisterMetering()
            .AddHttpClient()
            .AddDefaultHttpClientMetering()
            .BuildServiceProvider();

        var requestMetadataContext = sp.GetRequiredService<IOutgoingRequestContext>();

        var requestMetadata = new RequestMetadata
        {
            DependencyName = "success_service",
            RequestRoute = "/foo"
        };

        requestMetadataContext?.SetRequestMetadata(requestMetadata);

        Assert.NotNull(requestMetadataContext);
    }

    [Fact]
    public static async Task AddDefaultHttpClientMetering_WithDownstreamDependencyMetadata_UsesIt()
    {
        var dependencyMetadata = new TestDownstreamDependencyMetadata();
        var downstreamDependencyMetadataManagerMock = new Mock<IDownstreamDependencyMetadataManager>();
        downstreamDependencyMetadataManagerMock
            .Setup(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>()))
            .Returns(It.IsAny<RequestMetadata>());

        using var sp = new ServiceCollection()
            .RegisterMetering()
            .AddHttpClient()
            .AddDefaultHttpClientMetering()
            .AddDownstreamDependencyMetadata<TestDownstreamDependencyMetadata>()
            .AddSingleton(downstreamDependencyMetadataManagerMock.Object)
            .BlockRemoteCall()
            .BuildServiceProvider();

        var client = sp
             .GetRequiredService<IHttpClientFactory>()
             .CreateClient(nameof(AddDefaultHttpClientMetering_WithDownstreamDependencyMetadata_UsesIt));

        _ = await client.GetAsync("https://contoso.com");

        downstreamDependencyMetadataManagerMock.Verify(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>()), Times.Once);
    }

    [Fact]
    public static async Task AddtHttpClientMetering_WithDownstreamDependencyMetadata_UsesIt()
    {
        var downstreamDependencyMetadataManagerMock = new Mock<IDownstreamDependencyMetadataManager>();
        downstreamDependencyMetadataManagerMock
            .Setup(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>()))
            .Returns(It.IsAny<RequestMetadata>());

        using var sp = new ServiceCollection()
            .RegisterMetering()
            .AddHttpClient(nameof(AddtHttpClientMetering_WithDownstreamDependencyMetadata_UsesIt))
            .AddHttpClientMetering()
            .Services
            .AddDownstreamDependencyMetadata<TestDownstreamDependencyMetadata>()
            .AddSingleton(downstreamDependencyMetadataManagerMock.Object)
            .BlockRemoteCall()
            .BuildServiceProvider();

        var client = sp
             .GetRequiredService<IHttpClientFactory>()
             .CreateClient(nameof(AddtHttpClientMetering_WithDownstreamDependencyMetadata_UsesIt));

        _ = await client.GetAsync("https://contoso.com");

        downstreamDependencyMetadataManagerMock.Verify(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>()), Times.Once);
    }
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
}
