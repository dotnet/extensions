// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Latency;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Test;

public class AcceptanceTest
{
    private static void SetupServices(IHostBuilder builder)
    {
        builder.ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddRouting()
                    .AddLatencyContext()
                    .AddRequestCheckpoint()
                    .AddScoped(p => p.GetRequiredService<ILatencyContextProvider>().CreateContext())));
    }

    [Fact]
    public async Task RequestCheckpoint_CanMeasureMiddlewarePipeTime()
    {
        var reachedLambda = false;
        var exitPipelineValue = 0d;
        var responseProcessedValue = 0d;

        using var host = await FakeHost.CreateBuilder()
            .Configure(SetupServices)
            .ConfigureWebHost(webBuilder => webBuilder.Configure(app =>
            {
                app.Use(async (context, next) =>
                {
                    var latencyContext = context.RequestServices.GetRequiredService<ILatencyContext>();
                    await next.Invoke().ConfigureAwait(false);
                    latencyContext.TryGetCheckpoint(RequestCheckpointConstants.ElapsedTillPipelineExitMiddleware, out var exitPipeline, out var exitPipelineFreq);
                    latencyContext.TryGetCheckpoint(RequestCheckpointConstants.ElapsedResponseProcessed, out var responseProcessed, out var responseProcessedFreq);

                    reachedLambda = true;
                    exitPipelineValue = ((double)exitPipeline / exitPipelineFreq) * 1000;
                    responseProcessedValue = ((double)responseProcessed / responseProcessedFreq) * 1000;
                });

                app.UseRouting();
                app.UseRequestCheckpoint();
                app.UseEndpoints(endpoints => endpoints.MapGet("/", async context => await context.Response.WriteAsync("Hello World!")));
            }))
            .StartAsync();

        _ = await host.GetTestClient().GetAsync("/").ConfigureAwait(false);

        Assert.True(reachedLambda);
        Assert.InRange(exitPipelineValue, 1, 10_000);
        Assert.InRange(responseProcessedValue, 1, 10_000);
    }

    [Fact]
    public async Task RequestCheckpointMiddleware_Does_Not_Throw_When_ServerTiming_Header_Is_Already_Set()
    {
        var exitPipelineValue = 0d;
        var responseProcessedValue = 0d;
        var alreadySetServerTimingHeader = new StringValues("Already-Set-Some-Header;blabla");

        using var host = await FakeHost.CreateBuilder()
            .Configure(SetupServices)
            .ConfigureWebHost(webBuilder => webBuilder.Configure(app =>
            {
                app.Use(async (context, next) =>
                {
                    var latencyContext = context.RequestServices.GetRequiredService<ILatencyContext>();
                    await next.Invoke().ConfigureAwait(false);
                    latencyContext.TryGetCheckpoint(RequestCheckpointConstants.ElapsedTillPipelineExitMiddleware, out var exitPipeline, out var exitPipelineFreq);
                    latencyContext.TryGetCheckpoint(RequestCheckpointConstants.ElapsedResponseProcessed, out var responseProcessed, out var responsedProcessedFreq);
                    exitPipelineValue = ((double)exitPipeline / exitPipelineFreq) * 1000;
                    responseProcessedValue = ((double)responseProcessed / responsedProcessedFreq) * 1000;
                });

                app.Use((ctx, next) =>
                {
                    ctx.Response.Headers.Append("Server-Timing", alreadySetServerTimingHeader);

                    return next();
                });

                app.UseRouting();
                app.UseRequestCheckpoint();
                app.UseEndpoints(endpoints => endpoints.MapGet("/", async context => await context.Response.WriteAsync("Hello World!")));
            }))
            .StartAsync();

        HttpResponseMessage? response = null;

        var e = await Record.ExceptionAsync(async () => response = await host.GetTestClient().GetAsync("/").ConfigureAwait(false));

        Assert.Null(e);
        Assert.NotNull(response);

        var h = response.Headers
            .GetValues("Server-Timing")
            .FirstOrDefault();

        Assert.NotNull(h);
        Assert.NotEmpty(h);
        Assert.Contains(alreadySetServerTimingHeader!, h);
    }

    [Fact]
    public async Task MiddlewareTest_ReturnsNotFoundForRequest()
    {
        using var host = await FakeHost.CreateBuilder()
            .Configure(SetupServices)
            .ConfigureWebHost(webBuilder => webBuilder.Configure(app => app.UseRequestCheckpoint()))
            .StartAsync();

        using var response = await host.GetTestServer().CreateClient().GetAsync("/Path");

        var timeHeaders = response.Headers.GetValues("Server-Timing").ToArray();
        var metricFragments = Assert.Single(timeHeaders).Split('=', 2);
        Assert.Equal(2, metricFragments.Length);
        Assert.Equal("reqlatency;dur", metricFragments[0]);
        Assert.True(long.TryParse(metricFragments[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value));
        Assert.InRange(value, 0, 10_000);
    }
}
