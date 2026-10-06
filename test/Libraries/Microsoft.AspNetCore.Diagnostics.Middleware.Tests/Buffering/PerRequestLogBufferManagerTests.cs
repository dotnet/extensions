// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Diagnostics.Buffering;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Buffering.Test;

public class PerRequestLogBufferManagerTests
{
    private const string HostingDiagnosticsCategory = "Microsoft.AspNetCore.Hosting.Diagnostics";
    private const int RequestFinishedEventId = 2;
    private static readonly TimeSpan _logTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task WhenRequestEnds_RequestFinishedIsLogged()
    {
        using IHost host = await FakeHost.CreateBuilder()
            .ConfigureLogging(builder => builder
                .AddFilter(HostingDiagnosticsCategory, LogLevel.Information)
                .AddPerIncomingRequestBuffer(LogLevel.Debug))
            .ConfigureWebHost(builder => builder
                .UseTestServer()
                .Configure(app => app.Run(context => context.Response.WriteAsync("Hello"))))
            .StartAsync();

        FakeLogCollector logCollector = host.Services.GetFakeLogCollector();
        using HttpClient client = host.GetTestClient();

        using HttpResponseMessage response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // "Request finished" is logged after the response has been sent.
        await WaitForLogRecordAsync(
            logCollector,
            record => record.Category == HostingDiagnosticsCategory && record.Id.Id == RequestFinishedEventId);

        await host.StopAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WhenRequestServicesIsNull_TryEnqueueUsesGlobalBuffer(bool globalBufferResult)
    {
        var globalBuffer = new FakeGlobalLogBuffer { TryEnqueueResult = globalBufferResult };
        PerRequestLogBufferManager bufferManager = CreateBufferManagerWithNullRequestServices(globalBuffer);
        IBufferedLogger bufferedLogger = Mock.Of<IBufferedLogger>();
        var logEntry = new LogEntry<string>(LogLevel.Information, "test", new EventId(1), "state", null, static (state, _) => state);

        bool result = bufferManager.TryEnqueue(bufferedLogger, logEntry);

        Assert.Equal(globalBufferResult, result);
        Assert.Equal(1, globalBuffer.TryEnqueueCount);
        Assert.Same(bufferedLogger, globalBuffer.LastBufferedLogger);
        Assert.Equal(logEntry.Category, globalBuffer.LastCategory);
    }

    [Fact]
    public void WhenRequestServicesIsNull_FlushFlushesGlobalBuffer()
    {
        var globalBuffer = new FakeGlobalLogBuffer();
        PerRequestLogBufferManager bufferManager = CreateBufferManagerWithNullRequestServices(globalBuffer);

        bufferManager.Flush();

        Assert.Equal(1, globalBuffer.FlushCount);
    }

    private static PerRequestLogBufferManager CreateBufferManagerWithNullRequestServices(GlobalLogBuffer globalBuffer)
    {
        // DefaultHttpContext has no service scope factory, so its RequestServices is null.
        var httpContext = new DefaultHttpContext();
        Assert.Null(httpContext.RequestServices);

        return new PerRequestLogBufferManager(
            globalBuffer,
            Mock.Of<IHttpContextAccessor>(accessor => accessor.HttpContext == httpContext),
            Mock.Of<IOptionsMonitor<PerRequestLogBufferingOptions>>());
    }

    private static async Task WaitForLogRecordAsync(FakeLogCollector logCollector, Func<FakeLogRecord, bool> predicate)
    {
        var totalTimeWaiting = TimeSpan.Zero;
        var spinTime = TimeSpan.FromMilliseconds(50);
        while (totalTimeWaiting < _logTimeout)
        {
            if (logCollector.GetSnapshot().Any(predicate))
            {
                return;
            }

            await Task.Delay(spinTime);
            totalTimeWaiting += spinTime;
        }

        throw new TimeoutException("The expected log record wasn't emitted before the timeout was reached.");
    }

    private sealed class FakeGlobalLogBuffer : GlobalLogBuffer
    {
        public bool TryEnqueueResult { get; set; }

        public int TryEnqueueCount { get; private set; }

        public IBufferedLogger? LastBufferedLogger { get; private set; }

        public string? LastCategory { get; private set; }

        public int FlushCount { get; private set; }

        public override void Flush() => FlushCount++;

        public override bool TryEnqueue<TState>(IBufferedLogger bufferedLogger, in LogEntry<TState> logEntry)
        {
            TryEnqueueCount++;
            LastBufferedLogger = bufferedLogger;
            LastCategory = logEntry.Category;
            return TryEnqueueResult;
        }
    }
}
#endif
