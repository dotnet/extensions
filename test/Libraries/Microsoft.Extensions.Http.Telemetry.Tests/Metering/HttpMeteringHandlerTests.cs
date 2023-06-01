// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
#if !NETFRAMEWORK
using System.Diagnostics;
#endif
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
#if !NETFRAMEWORK
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
#endif
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

public sealed partial class HttpMeteringHandlerTests : IDisposable
{
    private const long DefaultClockAdvanceMs = 200;
    private const string DelayPropertyName = nameof(DelayPropertyName);

    private static readonly Uri _failureUri = new("https://www.example-failure.com/foo?bar");
    private static readonly Uri _failure1Uri = new("https://www.example-failure1.com/foo?bar");
    private static readonly Uri _failure2Uri = new("https://www.example-failure2.com/foo?bar");
    private static readonly Uri _failure3Uri = new("https://www.example-failure3.com/foo?bar");
    private static readonly Uri _internalServerErrorUri = new("https://www.example-failure.com/internalServererror");
    private static readonly Uri _expectedFailureUri = new("https://www.example-expectedfailure.com/foo?bar");
    private static readonly Uri _successfulUri = new("https://www.example-success.com/foo?bar");

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
    public void Handler_DoesntDisposeUnderlyingMeter_WhenMeterAndDisposeFalsePassed()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var handler = new OverriddenHttpMeteringHandler(meter, Array.Empty<IOutgoingRequestMetricEnricher>());
        var disposed = CheckHandlerUnderlyingMeterDisposed(counterName =>
        {
            var counter = meter.CreateCounter<int>(counterName);
            counter.Add(100_500);
            handler.ExternalDispose(false);
        });

        Assert.False(disposed, "The underlying Meter in the handler should not be disposed");
    }

    [Fact]
    public void Handler_DoesntDisposeUnderlyingMeter_WhenMeterPassed()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        var disposed = CheckHandlerUnderlyingMeterDisposed(counterName =>
        {
            using var handler = new HttpMeteringHandler(meter, Array.Empty<IOutgoingRequestMetricEnricher>());
            var counter = meter.CreateCounter<int>(counterName);
            counter.Add(100_500);
        });

        Assert.False(disposed, "The underlying Meter in the handler should not be disposed");
    }

    private static bool CheckHandlerUnderlyingMeterDisposed(Action<string> testAction)
    {
        var counterName = Guid.NewGuid().ToString();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished += (instrument, listener) =>
        {
            if (instrument.Name == counterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        var disposed = false;
        meterListener.MeasurementsCompleted += (instrument, _) =>
        {
            if (instrument.Name == counterName)
            {
                disposed = true;
            }
        };

        meterListener.Start();
        testAction(counterName);

        return disposed;
    }

    [Fact]
    public void SendAsync_Success_NoNamesSet()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);

        using var client = CreateClientWithHandler(meter);

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, _successfulUri);

        using var _ = client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token).Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;
        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal(TelemetryConstants.Unknown, latest.GetDimension(Metric.DependencyName));
        Assert.Equal($"PUT {TelemetryConstants.Unknown}", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.Success.ToString(), latest.GetDimension(Metric.RspResultCategory));
    }

    [Fact]
    public void SendAsync_Success()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        using var client = CreateClientWithHandler(meter);

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _successfulUri);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            DependencyName = "success_service",
            RequestRoute = "/foo"
        });

        using var _ = client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token).Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;
        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal($"GET /foo", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.Success.ToString(), latest.GetDimension(Metric.RspResultCategory));
    }

    [Fact]
    public void PerfStopwatch_ReturnsTotalMilliseconds_InsteadOfFraction()
    {
        const long TimeAdvanceMs = 1500L; // We need to use any value greater than 1000 (1 second)
        const string ServiceName = "success_service";

        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        using var client = CreateClientWithHandler(meter);

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _successfulUri);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            DependencyName = ServiceName,
            RequestRoute = "/foo"
        });

        _clockAdvanceMs = TimeAdvanceMs;

        using var _ = client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token).Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.Success.ToString(), latest.GetDimension(Metric.RspResultCategory));
    }

    [Fact]
    public async Task SendAsync_Exception()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        using var client = CreateClientWithHandler(meter);

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _failureUri);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            DependencyName = "failure_service",
            RequestRoute = "/foo/failure",
            RequestName = "TestRequestName"
        });

        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            using var _ = await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token);
        });

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-failure.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("failure_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("POST TestRequestName", latest.GetDimension(Metric.ReqName));
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.Failure.ToInvariantString(), latest.GetDimension(Metric.RspResultCategory));
    }

    [Fact]
    public void SendAsync_SetReqMetadata_OnAsyncContext_Success()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var requestMetadataContextMock = new Mock<IOutgoingRequestContext>();
        using var client = CreateClientWithHandler(meter, requestMetadataContextMock.Object);

        var requestMetadata = new RequestMetadata
        {
            DependencyName = "success_service",
            RequestRoute = "/foo"
        };
        requestMetadataContextMock.Setup(m => m.RequestMetadata).Returns(requestMetadata);

        using var _ = client.GetAsync(_successfulUri, _cancellationTokenSource.Token).Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.Success.ToString(), latest.GetDimension(Metric.RspResultCategory));
    }

    [Fact]
    public void PerfStopwatch_SetReqMetadata_OnAsyncContext_ReturnsTotalMilliseconds_InsteadOfFraction()
    {
        const long TimeAdvanceMs = 1500L; // We need to use any value greater than 1000 (1 second)
        const string ServiceName = "success_service";

        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var requestMetadataContextMock = new Mock<IOutgoingRequestContext>();
        using var client = CreateClientWithHandler(meter, requestMetadataContextMock.Object);

        var requestMetadata = new RequestMetadata
        {
            DependencyName = ServiceName,
            RequestRoute = "/foo"
        };

        requestMetadataContextMock.Setup(m => m.RequestMetadata).Returns(requestMetadata);

        _clockAdvanceMs = TimeAdvanceMs;

        using var _ = client.GetAsync(_successfulUri, _cancellationTokenSource.Token).Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.Success.ToString(), latest.GetDimension(Metric.RspResultCategory));
    }

    [Fact]
    public async Task SendAsync_SetReqMetadata_OnAsyncContext_Exception()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var requestMetadataContextMock = new Mock<IOutgoingRequestContext>();
        using var client = CreateClientWithHandler(meter, requestMetadataContextMock.Object);

        var requestMetadata = new RequestMetadata
        {
            DependencyName = "failure_service",
            RequestRoute = "/foo/failure",
            RequestName = "TestRequestName"
        };

        requestMetadataContextMock.Setup(m => m.RequestMetadata).Returns(requestMetadata);

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _failureUri);
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            using var _ = await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token);
        });

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-failure.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("failure_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("POST TestRequestName", latest.GetDimension(Metric.ReqName));
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.Failure.ToInvariantString(), latest.GetDimension(Metric.RspResultCategory));
    }

    [Fact]
    public void SendAsync_WithDownstreamDependencyMetadata_OnAsyncContext_Success()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var downstreamDependencyMetadataManagerMock = new Mock<IDownstreamDependencyMetadataManager>();
        using var client = CreateClientWithHandler(meter, downstreamDependencyMetadataManager: downstreamDependencyMetadataManagerMock.Object);

        var requestMetadata = new RequestMetadata
        {
            DependencyName = "success_service",
            RequestRoute = "/foo"
        };

        downstreamDependencyMetadataManagerMock
            .Setup(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>()))
            .Returns(requestMetadata);

        using var _ = client.GetAsync(_successfulUri, _cancellationTokenSource.Token).Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.Success.ToString(), latest.GetDimension(Metric.RspResultCategory));
    }

    [Fact]
    public void PerfStopwatch_WithDownstreamDependencyMetadata_OnAsyncContext_ReturnsTotalMilliseconds_InsteadOfFraction()
    {
        const long TimeAdvanceMs = 1500L; // We need to use any value greater than 1000 (1 second)
        const string ServiceName = "success_service";

        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var downstreamDependencyMetadataManagerMock = new Mock<IDownstreamDependencyMetadataManager>();
        using var client = CreateClientWithHandler(meter, downstreamDependencyMetadataManager: downstreamDependencyMetadataManagerMock.Object);

        var requestMetadata = new RequestMetadata
        {
            DependencyName = ServiceName,
            RequestRoute = "/foo"
        };

        downstreamDependencyMetadataManagerMock.Setup(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>())).Returns(requestMetadata);

        _clockAdvanceMs = TimeAdvanceMs;

        using var _ = client.GetAsync(_successfulUri, _cancellationTokenSource.Token).Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.Success.ToString(), latest.GetDimension(Metric.RspResultCategory));
    }

    [Fact]
    public async Task SendAsync_WithDownstreamDependencyMetadata_OnAsyncContext_Exception()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        var dependencyDataManagerMock = new Mock<IDownstreamDependencyMetadataManager>();
        using var client = CreateClientWithHandler(meter, downstreamDependencyMetadataManager: dependencyDataManagerMock.Object);

        var requestMetadata = new RequestMetadata
        {
            DependencyName = "failure_service",
            RequestRoute = "/foo/failure",
            RequestName = "TestRequestName"
        };
        dependencyDataManagerMock.Setup(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>())).Returns(requestMetadata);

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _failureUri);
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            using var _ = await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token);
        });

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-failure.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("failure_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("POST TestRequestName", latest.GetDimension(Metric.ReqName));
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.Failure.ToInvariantString(), latest.GetDimension(Metric.RspResultCategory));
    }

    [Fact]
    public void GetHostName_Returns_Unknown_For_Empty_RequestUri()
    {
        using var requestMessage = new HttpRequestMessage();
        var c = HttpMeteringHandler.GetHostName(requestMessage);

        Assert.Equal(TelemetryConstants.Unknown, c);
    }

    [Fact]
    public async Task SendAsync_MultiEnrich()
    {
        for (int i = 1; i <= 14; i++)
        {
            using var meter = new Meter<HttpMeteringHandler>();
            using var metricCollector = new MetricCollector(meter);
            using var client = CreateClientWithHandler(meter, new List<IOutgoingRequestMetricEnricher>
                {
                    new TestEnricher(i),
                });

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _successfulUri);
            httpRequestMessage.SetRequestMetadata(new RequestMetadata
            {
                DependencyName = "success_service",
                RequestRoute = "/foo"
            });

            using var _ = await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token);

            var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

            Assert.NotNull(latest);
            Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
            Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
            Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
            Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
            Assert.Equal(HttpRequestResultType.Success.ToString(), latest.GetDimension(Metric.RspResultCategory));

            for (int j = 0; j < i; j++)
            {
                Assert.Equal($"test_value_{j + 1}", latest.GetDimension($"test_property_{j + 1}"));
            }
        }
    }

    [Fact]
    public async Task SendAsync_MultiEnrich_UsingMeter()
    {
        for (int i = 1; i <= 14; i++)
        {
            using var meter = new Meter<HttpMeteringHandler>();
            using var metricCollector = new MetricCollector(meter);
            using var handler = new HttpMeteringHandler(meter, new[] { new TestEnricher(i) })
            {
                InnerHandler = new TestHandlerStub(InnerHandlerFunction)
            };

            using var client = new System.Net.Http.HttpClient(handler);

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _successfulUri);
            httpRequestMessage.SetRequestMetadata(new RequestMetadata
            {
                DependencyName = "success_service",
                RequestRoute = "/foo"
            });

            using var _ = await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token);

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
        using var client = CreateClientWithHandler(meter, new List<IOutgoingRequestMetricEnricher>
            {
                new TestEnricher(2),
                new TestEnricher(2, "2"),
            });

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _successfulUri);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            DependencyName = "success_service",
            RequestRoute = "/foo"
        });

        using var _ = await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token);
        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.Success.ToString(), latest.GetDimension(Metric.RspResultCategory));
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
        using var client = CreateClientWithHandler(meter, new List<IOutgoingRequestMetricEnricher>
            {
                new PropertyBagEdgeCaseEnricher(),
            });

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _successfulUri);
        httpRequestMessage.SetRequestMetadata(new RequestMetadata
        {
            DependencyName = "success_service",
            RequestRoute = "/foo"
        });

        using var _ = await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token);
        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;

        Assert.NotNull(latest);
        Assert.Equal("www.example-success.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal("success_service", latest.GetDimension(Metric.DependencyName));
        Assert.Equal("GET /foo", latest.GetDimension(Metric.ReqName));
        Assert.Equal(201, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.Success.ToString(), latest.GetDimension(Metric.RspResultCategory));
        Assert.Equal("test_val", latest.GetDimension("non_null_object_property"));
    }

    [Fact]
    public void HttpMeteringHandler_Fail_16DEnrich()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            using var _ = CreateClientWithHandler(meter, new List<IOutgoingRequestMetricEnricher>
            {
                new TestEnricher(16)
            });
        });
    }

    [Fact]
    public void HttpMeteringHandler_Fail_RepeatCustomDimensions()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        Assert.Throws<ArgumentException>(() =>
        {
            using var _ = CreateClientWithHandler(meter, new List<IOutgoingRequestMetricEnricher>
            {
                new TestEnricher(1),
                new TestEnricher(1),
            });
        });
    }

    [Fact]
    public void HttpMeteringHandler_Fail_RepeatDefaultDimensions()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        Assert.Throws<ArgumentException>(() =>
        {
            using var _ = CreateClientWithHandler(meter, new List<IOutgoingRequestMetricEnricher>
            {
                new SameDefaultDimEnricher(),
            });
        });
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

        Assert.Equal(2, enrichersCollection.Count());
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Handler will be disposed with HttpClient")]
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

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Handler will be disposed with HttpClient")]
    private System.Net.Http.HttpClient CreateClientWithHandler(
        Meter<HttpMeteringHandler> meter,
        IOutgoingRequestContext? requestMetadataContext = null,
        IDownstreamDependencyMetadataManager? downstreamDependencyMetadataManager = null)
    {
        var handler = new HttpMeteringHandler(meter, Array.Empty<IOutgoingRequestMetricEnricher>(), requestMetadataContext, downstreamDependencyMetadataManager)
        {
            InnerHandler = new TestHandlerStub(InnerHandlerFunction),
            TimeProvider = _fakeTimeProvider,
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
        else if (request.RequestUri == _failure1Uri)
        {
            throw new TaskCanceledException("Timeout");
        }
        else if (request.RequestUri == _failure2Uri)
        {
            throw new InvalidOperationException("Invalid Operation");
        }
        else if (request.RequestUri == _failure3Uri)
        {
#if NET6_0_OR_GREATER
            throw new HttpRequestException("Something went wrong", null, HttpStatusCode.BadGateway);
#else
            throw new HttpRequestException("Something went wrong");
#endif
        }

        HttpResponseMessage response;
        if (request.RequestUri == _expectedFailureUri)
        {
            response = new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest };
        }
        else if (request.RequestUri == _internalServerErrorUri)
        {
            response = new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError };
        }
        else
        {
            response = new HttpResponseMessage { StatusCode = HttpStatusCode.Created };
        }

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

        using var httpClient = httpClientFactory.CreateClient();

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

        using var _ = await client.GetAsync("https://www.bing.com");

        downstreamDependencyMetadataManagerMock.Verify(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>()), Times.Once);
    }

    [Fact]
    public static async Task AddHttpClientMetering_WithDownstreamDependencyMetadata_UsesIt()
    {
        var downstreamDependencyMetadataManagerMock = new Mock<IDownstreamDependencyMetadataManager>();
        downstreamDependencyMetadataManagerMock
            .Setup(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>()))
            .Returns(It.IsAny<RequestMetadata>());

        using var sp = new ServiceCollection()
            .RegisterMetering()
            .AddHttpClient(nameof(AddHttpClientMetering_WithDownstreamDependencyMetadata_UsesIt))
            .AddHttpClientMetering()
            .Services
            .AddDownstreamDependencyMetadata<TestDownstreamDependencyMetadata>()
            .AddSingleton(downstreamDependencyMetadataManagerMock.Object)
            .BlockRemoteCall()
            .BuildServiceProvider();

        var client = sp
             .GetRequiredService<IHttpClientFactory>()
             .CreateClient(nameof(AddHttpClientMetering_WithDownstreamDependencyMetadata_UsesIt));

        using var _ = await client.GetAsync("https://www.bing.com");

        downstreamDependencyMetadataManagerMock.Verify(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>()), Times.Once);
    }

#if !NETFRAMEWORK
    [Fact]
    public static void AddHttpClientMeteringForAllHttpClients_NullServiceCollection_Throws()
    {
        IServiceCollection services = null!;
        Assert.Throws<ArgumentNullException>(() => services.AddHttpClientMeteringForAllHttpClients());
    }

    [Fact]
    public static void AddHttpClientMeteringForAllHttpClients_WithDownstreamDependencyMetadata_UsesIt()
    {
        var downstreamDependencyMetadataManagerMock = new Mock<IDownstreamDependencyMetadataManager>();
        downstreamDependencyMetadataManagerMock
            .Setup(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>()))
            .Returns(It.IsAny<RequestMetadata>());

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices((_, services) => services
                .RegisterMetering()
                .AddHttpClient()
                .AddHttpClientMeteringForAllHttpClients()
                .AddDownstreamDependencyMetadata<TestDownstreamDependencyMetadata>()
                .AddSingleton(downstreamDependencyMetadataManagerMock.Object))
            .Build();

        host.Start();

        var client = host.Services
             .GetRequiredService<IHttpClientFactory>()
             .CreateClient(nameof(AddDefaultHttpClientMetering_WithDownstreamDependencyMetadata_UsesIt));

        using var _ = client.GetAsync("https://www.bing.com").Result;
        HttpClientMeteringListener.UsingDiagnosticsSource = false;

        downstreamDependencyMetadataManagerMock.Verify(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>()), Times.AtLeastOnce);
        HttpClientMeteringListener.UsingDiagnosticsSource = false;
    }

    [Fact]
    public static void AddHttpClientMeteringForAllHttpClients_EmitMetrics_OnException()
    {
        var downstreamDependencyMetadataManagerMock = new Mock<IDownstreamDependencyMetadataManager>();
        downstreamDependencyMetadataManagerMock
            .Setup(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>()))
            .Returns(It.IsAny<RequestMetadata>());

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices((_, services) => services
                .AddHttpClient()
                .AddHttpClientMeteringForAllHttpClients()
                .AddDownstreamDependencyMetadata<TestDownstreamDependencyMetadata>()
                .AddSingleton(downstreamDependencyMetadataManagerMock.Object))
            .Build();

        host.Start();
        var client = host.Services
             .GetRequiredService<IHttpClientFactory>()
             .CreateClient(nameof(AddDefaultHttpClientMetering_WithDownstreamDependencyMetadata_UsesIt));

        var meter = host.Services.GetRequiredService<Meter<HttpMeteringHandler>>();
        using var meterCollector = new MetricCollector(meter);

        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        Assert.Throws<AggregateException>(() => client.GetAsync("https://localhost:12345").Result);
        HttpClientMeteringListener.UsingDiagnosticsSource = false;

        var record = meterCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;
        Assert.NotNull(record);
        Assert.True(record.Value > 0);
        Assert.True(record.Value <= (DateTimeOffset.UtcNow - startTime).TotalMilliseconds);
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, record.GetDimension(Metric.RspResultCode));
        HttpClientMeteringListener.UsingDiagnosticsSource = false;
    }

    [Fact]
    public static void AddHttpClientMeteringForAllHttpClients_EmitMetrics_OnTaskCancelledException()
    {
        var downstreamDependencyMetadataManagerMock = new Mock<IDownstreamDependencyMetadataManager>();
        downstreamDependencyMetadataManagerMock
            .Setup(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>()))
            .Returns(It.IsAny<RequestMetadata>());

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices((_, services) => services
                .AddHttpClient()
                .AddHttpClientMeteringForAllHttpClients()
                .AddDownstreamDependencyMetadata<TestDownstreamDependencyMetadata>()
                .AddSingleton(downstreamDependencyMetadataManagerMock.Object))
            .Build();

        host.Start();
        var client = host.Services
             .GetRequiredService<IHttpClientFactory>()
             .CreateClient(nameof(AddDefaultHttpClientMetering_WithDownstreamDependencyMetadata_UsesIt));

        var meter = host.Services.GetRequiredService<Meter<HttpMeteringHandler>>();
        using var meterCollector = new MetricCollector(meter);

        DateTimeOffset startTime = DateTimeOffset.UtcNow;

        var cts = new CancellationTokenSource();
        using var testObserver = new RequestCancellationTestObserver(cts);

        Assert.Throws<AggregateException>(() => client.GetAsync("https://www.bing.com", cts.Token).Result);

        HttpClientMeteringListener.UsingDiagnosticsSource = false;

        var record = meterCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;
        Assert.NotNull(record);
        Assert.True(record.Value >= 0);
        Assert.True(record.Value <= (DateTimeOffset.UtcNow - startTime).TotalMilliseconds);
        Assert.Equal((int)HttpStatusCode.GatewayTimeout, record.GetDimension(Metric.RspResultCode));
        HttpClientMeteringListener.UsingDiagnosticsSource = false;
    }

    [Fact]
    public static void AddHttpClientMeteringForAllHttpClients_EmitMetrics_OnError()
    {
        var downstreamDependencyMetadataManagerMock = new Mock<IDownstreamDependencyMetadataManager>();
        downstreamDependencyMetadataManagerMock
            .Setup(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>()))
            .Returns(It.IsAny<RequestMetadata>());

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices((_, services) => services
                .AddHttpClient()
                .AddHttpClientMeteringForAllHttpClients()
                .AddDownstreamDependencyMetadata<TestDownstreamDependencyMetadata>()
                .AddSingleton(downstreamDependencyMetadataManagerMock.Object))
            .Build();

        host.Start();
        var client = host.Services
             .GetRequiredService<IHttpClientFactory>()
             .CreateClient(nameof(AddDefaultHttpClientMetering_WithDownstreamDependencyMetadata_UsesIt));

        var meter = host.Services.GetRequiredService<Meter<HttpMeteringHandler>>();
        using var meterCollector = new MetricCollector(meter);

        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        _ = client.GetAsync("https://www.bing.com/request").Result;
        HttpClientMeteringListener.UsingDiagnosticsSource = false;

        var record = meterCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;
        Assert.NotNull(record);
        Assert.True(record.Value > 0);
        Assert.True(record.Value <= (DateTimeOffset.UtcNow - startTime).TotalMilliseconds);
        Assert.Equal((int)HttpStatusCode.NotFound, record.GetDimension(Metric.RspResultCode));
        HttpClientMeteringListener.UsingDiagnosticsSource = false;
    }

    [Fact]
    public static void AddHttpClientMeteringForAllHttpClients_EmitMetrics_OnSuccessResponse()
    {
        var downstreamDependencyMetadataManagerMock = new Mock<IDownstreamDependencyMetadataManager>();
        downstreamDependencyMetadataManagerMock
            .Setup(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>()))
            .Returns(It.IsAny<RequestMetadata>());

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices((_, services) => services
                .AddHttpClient()
                .AddHttpClientMeteringForAllHttpClients()
                .AddDownstreamDependencyMetadata<TestDownstreamDependencyMetadata>()
                .AddSingleton(downstreamDependencyMetadataManagerMock.Object))
            .Build();

        host.Start();
        var client = host.Services
             .GetRequiredService<IHttpClientFactory>()
             .CreateClient(nameof(AddDefaultHttpClientMetering_WithDownstreamDependencyMetadata_UsesIt));

        var meter = host.Services.GetRequiredService<Meter<HttpMeteringHandler>>();
        using var meterCollector = new MetricCollector(meter);

        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        using var _ = client.GetAsync("https://www.bing.com").Result;
        HttpClientMeteringListener.UsingDiagnosticsSource = false;

        var record = meterCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;
        Assert.NotNull(record);
        Assert.True(record.Value > 0);
        Assert.True(record.Value <= (DateTimeOffset.UtcNow - startTime).TotalMilliseconds);
        Assert.Equal(200, record.GetDimension(Metric.RspResultCode));
        HttpClientMeteringListener.UsingDiagnosticsSource = false;
    }

    [Fact]
    public static void AddHttpClientMeteringForAllHttpClients_CaptureMetrics_ForNonHttpClientFactoryClients()
    {
        var downstreamDependencyMetadataManagerMock = new Mock<IDownstreamDependencyMetadataManager>();
        downstreamDependencyMetadataManagerMock
            .Setup(m => m.GetRequestMetadata(It.IsAny<HttpRequestMessage>()))
            .Returns(It.IsAny<RequestMetadata>());

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices((_, services) => services
                .AddHttpClientMeteringForAllHttpClients())
            .Build();

        host.Start();
        var meter = host.Services.GetRequiredService<Meter<HttpMeteringHandler>>();
        using var meterCollector = new MetricCollector(meter);

        using var client = new System.Net.Http.HttpClient();

        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        using var _ = client.GetAsync("https://www.bing.com").Result;
        HttpClientMeteringListener.UsingDiagnosticsSource = false;

        var record = meterCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;
        Assert.NotNull(record);
        Assert.True(record.Value > 0);
        Assert.True(record.Value <= (DateTimeOffset.UtcNow - startTime).TotalMilliseconds);
        Assert.Equal(200, record.GetDimension(Metric.RspResultCode));
        HttpClientMeteringListener.UsingDiagnosticsSource = false;
    }

    [Fact]
    public static void When_DiagSourceAndDelegatingHandler_BothConfigured_MetricsOnlyEmittedOnce()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices((_, services) => services
                .AddHttpClient()
                .AddDefaultHttpClientMetering()
                .AddHttpClientMeteringForAllHttpClients())
            .Build();

        host.Start();
        var client = host.Services
             .GetRequiredService<IHttpClientFactory>()
             .CreateClient(nameof(AddDefaultHttpClientMetering_WithDownstreamDependencyMetadata_UsesIt));

        var meter = host.Services.GetRequiredService<Meter<HttpMeteringHandler>>();
        using var meterCollector = new MetricCollector(meter);

        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        using var _ = client.GetAsync("https://www.bing.com").Result;
        HttpClientMeteringListener.UsingDiagnosticsSource = false;

        var records = meterCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!;
        Assert.NotNull(records);
        Assert.Equal(1, records.AllValues.Count);

        var record = records.LatestWritten!;
        Assert.True(record.Value > 0);
        Assert.True(record.Value <= (DateTimeOffset.UtcNow - startTime).TotalMilliseconds);
        Assert.Equal(200, record.GetDimension(Metric.RspResultCode));
        HttpClientMeteringListener.UsingDiagnosticsSource = false;
    }

    [Fact]
    public void HttpClientDiagnosticsObserver_OnError_DoesNotThrow()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var httpMeteringHandler = new HttpMeteringHandler(meter, new List<IOutgoingRequestMetricEnricher>());
        using var httpClientDiagnosticsObserver = new HttpClientDiagnosticObserver(new HttpClientRequestAdapter(httpMeteringHandler));

        Assert.NotNull(httpClientDiagnosticsObserver);
        httpClientDiagnosticsObserver.OnError(new NotSupportedException());
    }

    [Fact]
    public void HttpClientDiagnosticsObserver_OnNext_MultipleCalls_DoesNotThrow()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var httpMeteringHandler = new HttpMeteringHandler(meter, new List<IOutgoingRequestMetricEnricher>());
        using var httpClientDiagnosticsObserver = new HttpClientDiagnosticObserver(new HttpClientRequestAdapter(httpMeteringHandler));

        Assert.NotNull(httpClientDiagnosticsObserver);

        using var diagnosticsListener = new DiagnosticListener("HttpHandlerDiagnosticListener");
        httpClientDiagnosticsObserver.OnNext(diagnosticsListener);
        httpClientDiagnosticsObserver.OnNext(diagnosticsListener);
    }

    [Fact]
    public void HttpClientRequestAdapter_OnRequestStop_TaskStatusNotFaultedOrCancelled_ResponseNull_EmitInternalServerError()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var httpMeteringHandler = new HttpMeteringHandler(meter, new List<IOutgoingRequestMetricEnricher>());
        var httpClientRequestAdapter = new HttpClientRequestAdapter(httpMeteringHandler);

        Assert.NotNull(httpClientRequestAdapter);

        using var httpRequestMessage = new HttpRequestMessage();
        httpClientRequestAdapter.HttpClientListenerSubscribed(httpRequestMessage);
        httpClientRequestAdapter.OnRequestStart(httpRequestMessage);

        using var meterCollector = new MetricCollector(meter);

        httpClientRequestAdapter.OnRequestStop(null, httpRequestMessage, TaskStatus.RanToCompletion);

        var records = meterCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!;
        Assert.NotNull(records);
        Assert.Equal(1, records.AllValues.Count);

        var record = records.LatestWritten!;
        Assert.Equal((int)HttpStatusCode.InternalServerError, record.GetDimension(Metric.RspResultCode));
        HttpClientMeteringListener.UsingDiagnosticsSource = false;
    }
#endif

    [Fact]
    public void SendAsync_Failure_NoExceptionThrown()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        using var client = CreateClientWithHandler(meter);

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _internalServerErrorUri);

        using var _ = client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token).Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;
        Assert.NotNull(latest);
        Assert.Equal("www.example-failure.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal(TelemetryConstants.Unknown, latest.GetDimension(Metric.DependencyName));
        Assert.Equal($"GET {TelemetryConstants.Unknown}", latest.GetDimension(Metric.ReqName));
        Assert.Equal((int)HttpStatusCode.InternalServerError, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.Failure.ToInvariantString(), latest.GetDimension(Metric.RspResultCategory));
    }
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
}
