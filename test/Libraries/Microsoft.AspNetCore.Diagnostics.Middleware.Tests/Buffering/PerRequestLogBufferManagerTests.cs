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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
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
}
#endif
