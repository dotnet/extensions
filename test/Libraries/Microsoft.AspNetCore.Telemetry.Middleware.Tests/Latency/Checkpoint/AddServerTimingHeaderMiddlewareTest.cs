// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Latency;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Test;

public class AddServerTimingHeaderMiddlewareTest
{
    private static readonly RequestDelegate _stubRequestDelegate =
        static _ => Task.CompletedTask;

    [Fact]
    public async Task Middleware_ReturnsTotalMillisecondsElapsed_InsteadOfFraction()
    {
        const long TimeAdvanceMs = 1500L; // We need to use any value greater than 1000 (1 second)

        using FakeLatencyContext fakeLatencyContextController = new();
        Checkpoint checkpoint = new(RequestCheckpointConstants.ElapsedTillHeaders, TimeAdvanceMs, 1000);
        ArraySegment<Checkpoint> checkpoints = new(new[] { checkpoint });
        fakeLatencyContextController.LatencyData = new LatencyData(default, checkpoints, default, default, default);

        using var serviceProvider = new ServiceCollection()
            .AddSingleton<ILatencyContext>(_ => fakeLatencyContextController)
            .BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var fakeHttpResponseFeature = new FakeHttpResponseFeature();
        context.Features.Set<IHttpResponseFeature>(fakeHttpResponseFeature);

        var middleware = new AddServerTimingHeaderMiddleware();
        await middleware.InvokeAsync(context, _stubRequestDelegate);
        await fakeHttpResponseFeature.StartAsync();

        var header = context.Response.Headers[AddServerTimingHeaderMiddleware.ServerTimingHeaderName];
        Assert.NotEmpty(header);
        Assert.Equal($"reqlatency;dur={TimeAdvanceMs}", header[0]);
    }

    private sealed class FakeHttpResponseFeature : HttpResponseFeature
    {
        private Func<Task> _responseStartingAsync =
            static () => Task.CompletedTask;

        public override void OnStarting(Func<object, Task> callback, object state)
        {
            var prior = _responseStartingAsync;
            _responseStartingAsync = async () =>
            {
                await callback(state);
                await prior();
            };
        }

        public async Task StartAsync() => await _responseStartingAsync();
    }

    private sealed class FakeLatencyContext : ILatencyContext
    {
        public LatencyData LatencyData { get; set; }

        public void AddCheckpoint(CheckpointToken token) => throw new NotSupportedException();
        public void AddMeasure(MeasureToken token, long value) => throw new NotSupportedException();
        public void Dispose()
        {
            // Method intentionally left empty.
        }

        public void Freeze() => throw new NotSupportedException();
        public void RecordMeasure(MeasureToken _0, long _1) => throw new NotSupportedException();
        public void SetTag(TagToken _0, string _1) => throw new NotSupportedException();
    }
}
