// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Telemetry.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Latency;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Test.Internal;

public class RequestLatencyTelemetryMiddlewareTest
{
    [Fact]
    public async Task RequestLatency_GivenContext_InvokesOperations()
    {
        var ex1 = new TestExporter();
        var ex2 = new TestExporter();
        var m = new RequestLatencyTelemetryMiddleware(Options.Create(new RequestLatencyTelemetryOptions()), new List<ILatencyDataExporter> { ex1, ex2 });

        var lc = GetMockLatencyContext();
        var httpContextMock = GetHttpContext(lc.Object);

        var nextInvoked = false;
        await m.InvokeAsync(httpContextMock, (_) =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        lc.Verify(c => c.Freeze());
        Assert.True(nextInvoked);
        Assert.True(ex1.Invoked == 1);
        Assert.True(ex2.Invoked == 1);
    }

    [Fact]
    public async Task RequestLatency_NoExporter()
    {
        var lc = GetMockLatencyContext();
        var httpContextMock = GetHttpContext(lc.Object);
        var m = new RequestLatencyTelemetryMiddleware(Options.Create(new RequestLatencyTelemetryOptions()), Array.Empty<ILatencyDataExporter>());

        var nextInvoked = false;
        await m.InvokeAsync(httpContextMock, (_) =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        lc.Verify(c => c.Freeze());
        Assert.True(nextInvoked);
    }

    [Fact]
    public async Task RequestLatency_GivenTimeout_PassedToExport()
    {
        var exportTimeout = TimeSpan.FromSeconds(1);
        var ex1 = new TimeConsumingExporter(TimeSpan.FromSeconds(5));

        var m = new RequestLatencyTelemetryMiddleware(
            Options.Create(new RequestLatencyTelemetryOptions { LatencyDataExportTimeout = exportTimeout }),
            new List<ILatencyDataExporter> { ex1 });

        var lc = GetMockLatencyContext();
        var httpContextMock = GetHttpContext(lc.Object);

        var nextInvoked = false;
        await m.InvokeAsync(httpContextMock, (_) =>
            {
                nextInvoked = true;
                return Task.CompletedTask;
            });
        await httpContextMock.Response.CompleteAsync();

        lc.Verify(c => c.Freeze());
        Assert.True(nextInvoked);
    }

    private static HttpContext GetHttpContext(ILatencyContext latencyContext)
    {
        var httpContextMock = new DefaultHttpContext();

        var feature = new Mock<IHttpResponseFeature>();
        feature.Setup(m => m.OnCompleted(It.IsAny<Func<object, Task>>(), It.IsAny<object>()))
            .Callback<Func<object, Task>, object>((c, o) => c(o));
        httpContextMock.Features.Set(feature.Object);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(serviceProvider => serviceProvider.GetService(typeof(ILatencyContext)))
            .Returns(latencyContext);
        httpContextMock.RequestServices = serviceProviderMock.Object;

        return httpContextMock;
    }

    private static Mock<ILatencyContext> GetMockLatencyContext()
    {
        var cc = new Mock<ILatencyContext>();
        return cc;
    }

    private class TestExporter : ILatencyDataExporter
    {
        public int Invoked { get; private set; }

        public async Task ExportAsync(LatencyData latencyData, CancellationToken cancellationToken)
        {
            Invoked++;
            await Task.CompletedTask;
        }
    }

    private class TimeConsumingExporter : ILatencyDataExporter
    {
        public int Invoked { get; private set; }

        private readonly TimeSpan _timeSpanToDelay;

        public TimeConsumingExporter(TimeSpan timeSpanToDelay)
        {
            _timeSpanToDelay = timeSpanToDelay;
        }

        public async Task ExportAsync(LatencyData latencyData, CancellationToken cancellationToken)
        {
            Invoked++;

            var e = await Record.ExceptionAsync(() => Task.Delay(_timeSpanToDelay, cancellationToken));
            Assert.IsAssignableFrom<OperationCanceledException>(e);
        }
    }
}
