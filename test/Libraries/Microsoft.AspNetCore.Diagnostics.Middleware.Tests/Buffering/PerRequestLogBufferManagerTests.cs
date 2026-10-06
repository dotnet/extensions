// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
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
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (FakeLogRecord record in logCollector.GetLogsAsync(cts.Token))
        {
            if (record.Category == HostingDiagnosticsCategory && record.Id.Id == RequestFinishedEventId)
            {
                break;
            }
        }

        await host.StopAsync();
    }
}
#endif
