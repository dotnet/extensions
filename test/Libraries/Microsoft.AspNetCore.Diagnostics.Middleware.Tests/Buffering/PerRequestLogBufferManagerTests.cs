// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET9_0_OR_GREATER
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
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
                .Configure(_ => { }))
            .StartAsync();

        _ = await host.GetTestServer().SendAsync(_ => { });

        // "Request finished" is logged after the response has been sent.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        Assert.Contains(
            host.Services.GetFakeLogCollector().GetLogsAsync(cts.Token),
            record => record.Category == HostingDiagnosticsCategory && record.Id.Id == RequestFinishedEventId);

        await host.StopAsync();
    }
}
#endif
