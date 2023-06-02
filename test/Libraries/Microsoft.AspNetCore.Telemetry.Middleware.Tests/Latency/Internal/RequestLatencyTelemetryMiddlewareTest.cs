// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Telemetry.Internal;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Http.Telemetry;
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
        string serverName = "AppServer";
        var m = new RequestLatencyTelemetryMiddleware(Options.Create(new RequestLatencyTelemetryOptions()), new List<ILatencyDataExporter> { ex1, ex2 },
            Options.Create(new ApplicationMetadata { ApplicationName = serverName }));

        var lc = GetMockLatencyContext();
        var httpContextMock = GetHttpContext(lc.Object);
        var fakeHttpResponseFeature = new FakeHttpResponseFeature();
        httpContextMock.Features.Set<IHttpResponseFeature>(fakeHttpResponseFeature);

        var nextInvoked = false;
        await m.InvokeAsync(httpContextMock, (_) =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });
        await fakeHttpResponseFeature.StartAsync();
        lc.Verify(c => c.Freeze());
        var header = httpContextMock.Response.Headers[TelemetryConstants.ServerApplicationNameHeader];
        Assert.NotEmpty(header);
        Assert.Equal(serverName, header[0]);
        Assert.True(nextInvoked);
        Assert.True(ex1.Invoked == 1);
        Assert.True(ex2.Invoked == 1);
    }

    [Fact]
    public async Task RequestLatency_WithoutServiceMetadata_InvokesOperations()
    {
        var ex1 = new TestExporter();
        var ex2 = new TestExporter();
        var m = new RequestLatencyTelemetryMiddleware(Options.Create(new RequestLatencyTelemetryOptions()), new List<ILatencyDataExporter> { ex1, ex2 });

        var lc = GetMockLatencyContext();
        var httpContextMock = GetHttpContext(lc.Object);
        var fakeHttpResponseFeature = new FakeHttpResponseFeature();
        httpContextMock.Features.Set<IHttpResponseFeature>(fakeHttpResponseFeature);

        var nextInvoked = false;
        await m.InvokeAsync(httpContextMock, (_) =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });
        await fakeHttpResponseFeature.StartAsync();
        lc.Verify(c => c.Freeze());
        Assert.False(httpContextMock.Response.Headers.TryGetValue(TelemetryConstants.ServerApplicationNameHeader, out var val));
        Assert.True(nextInvoked);
        Assert.True(ex1.Invoked == 1);
        Assert.True(ex2.Invoked == 1);
    }

    [Fact]
    public async Task RequestLatency_NoServiceData_DoesNotAddHeader()
    {
        var ex1 = new TestExporter();
        var ex2 = new TestExporter();
        var m = new RequestLatencyTelemetryMiddleware(Options.Create(new RequestLatencyTelemetryOptions()), new List<ILatencyDataExporter> { ex1, ex2 }, Options.Create(new ApplicationMetadata()));

        var lc = GetMockLatencyContext();
        var httpContextMock = GetHttpContext(lc.Object);
        var fakeHttpResponseFeature = new FakeHttpResponseFeature();
        httpContextMock.Features.Set<IHttpResponseFeature>(fakeHttpResponseFeature);

        var nextInvoked = false;
        await m.InvokeAsync(httpContextMock, (_) =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });
        await fakeHttpResponseFeature.StartAsync();
        lc.Verify(c => c.Freeze());
        Assert.False(httpContextMock.Response.Headers.TryGetValue(TelemetryConstants.ServerApplicationNameHeader, out var val));
        Assert.True(nextInvoked);
        Assert.True(ex1.Invoked == 1);
        Assert.True(ex2.Invoked == 1);
    }

    [Fact]
    public async Task RequestLatency_NoExporter()
    {
        var lc = GetMockLatencyContext();
        var httpContextMock = GetHttpContext(lc.Object);
        var m = new RequestLatencyTelemetryMiddleware(Options.Create(new RequestLatencyTelemetryOptions()), Array.Empty<ILatencyDataExporter>(), Options.Create(new ApplicationMetadata()));

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
            new List<ILatencyDataExporter> { ex1 },
            Options.Create(new ApplicationMetadata()));

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

    private sealed class FakeHttpResponseFeature : HttpResponseFeature
    {
        private Func<Task> _responseStartingAsync =
            static () => Task.CompletedTask;

        public override void OnStarting(Func<object, Task> callback, object state)
        {
            ChainCallback(callback, state);
        }

        public override void OnCompleted(Func<object, Task> callback, object state)
        {
            ChainCallback(callback, state);
        }

        private void ChainCallback(Func<object, Task> callback, object state)
        {
            var prior = _responseStartingAsync;
            _responseStartingAsync = async () =>
            {
                await prior();
                await callback(state);
            };
        }

        public async Task StartAsync() => await _responseStartingAsync();
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
