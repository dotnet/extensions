// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Telemetry.Metering.Internal;
using Microsoft.Extensions.Http.Telemetry.Metering.Test.Internal;
using Microsoft.Extensions.Telemetry;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Extensions.Telemetry.Testing.Metering;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Metering.Test;

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
public sealed partial class HttpMeteringHandlerTests : IDisposable
{
    [Fact]
    public void SendAsync_expectedFailure_EmptyOutgoingRequestMetricEnricher()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        using var client = CreateClientWithHandler(meter);

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, _expectedFailureUri);

        using var _ = client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token).Result;

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;
        Assert.NotNull(latest);
        Assert.Equal("www.example-expectedfailure.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal(TelemetryConstants.Unknown, latest.GetDimension(Metric.DependencyName));
        Assert.Equal($"GET {TelemetryConstants.Unknown}", latest.GetDimension(Metric.ReqName));
        Assert.Equal(400, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.ExpectedFailure.ToInvariantString(), latest.GetDimension(Metric.RspResultCategory));
    }

    [Fact]
    public async Task SendAsync_TaskCanceledException()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        using var client = CreateClientWithHandler(meter);

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _failure1Uri);

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token));

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;
        Assert.NotNull(latest);
        Assert.Equal("www.example-failure1.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal(TelemetryConstants.Unknown, latest.GetDimension(Metric.DependencyName));
        Assert.Equal($"POST {TelemetryConstants.Unknown}", latest.GetDimension(Metric.ReqName));
        Assert.Equal((int)HttpStatusCode.GatewayTimeout, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.Failure.ToInvariantString(), latest.GetDimension(Metric.RspResultCategory));
    }

    [Fact]
    public async Task SendAsync_InvalidOperationException()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        using var client = CreateClientWithHandler(meter);

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _failure2Uri);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token));

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;
        Assert.NotNull(latest);
        Assert.Equal("www.example-failure2.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal(TelemetryConstants.Unknown, latest.GetDimension(Metric.DependencyName));
        Assert.Equal($"POST {TelemetryConstants.Unknown}", latest.GetDimension(Metric.ReqName));
        Assert.Equal((int)HttpStatusCode.InternalServerError, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.Failure.ToInvariantString(), latest.GetDimension(Metric.RspResultCategory));
    }

    [Fact]
    public async Task SendAsync_Exception_OutgoingRequestMetricEnricherOnly()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        using var client = CreateClientWithHandler(meter,
            new List<IOutgoingRequestMetricEnricher>
            {
                new TestEnricher(1),
            });

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _failureUri);
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
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, latest.GetDimension(Metric.RspResultCode));
        Assert.Equal(HttpRequestResultType.Failure.ToInvariantString(), latest.GetDimension(Metric.RspResultCategory));
        Assert.Equal("test_value_1", latest.GetDimension("test_property_1"));
    }

    [Fact]
    public async Task SendAsync_HttpRequestException()
    {
        using var meter = new Meter<HttpMeteringHandler>();
        using var metricCollector = new MetricCollector(meter);
        using var client = CreateClientWithHandler(meter);

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _failure3Uri);

        await Assert.ThrowsAsync<HttpRequestException>(async () => await client.SendAsync(httpRequestMessage, _cancellationTokenSource.Token));

        var latest = metricCollector.GetHistogramValues<long>(Metric.OutgoingRequestMetricName)!.LatestWritten!;
        Assert.NotNull(latest);
        Assert.Equal("www.example-failure3.com", latest.GetDimension(Metric.ReqHost));
        Assert.Equal(TelemetryConstants.Unknown, latest.GetDimension(Metric.DependencyName));
        Assert.Equal($"POST {TelemetryConstants.Unknown}", latest.GetDimension(Metric.ReqName));
#if NET8_0_OR_GREATER
        // Whilst these API are marked as NET6_0_OR_GREATER we don't build .NET 6.0,
        // and as such the API is available in .NET 8 onwards.
        Assert.Equal((int)HttpStatusCode.BadGateway, latest.GetDimension(Metric.RspResultCode));
#else
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, latest.GetDimension(Metric.RspResultCode));
#endif
        Assert.Equal(HttpRequestResultType.Failure.ToInvariantString(), latest.GetDimension(Metric.RspResultCategory));
    }
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
}
