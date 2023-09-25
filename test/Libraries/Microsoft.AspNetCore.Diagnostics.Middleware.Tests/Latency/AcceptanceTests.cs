// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Latency;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.Latency.Test;

public class AcceptanceTests
{
    [Fact]
    public async Task RequestLatency_LatencyContextIsStarted()
    {
        bool isInLambda = false;
        string checkpointName = "testc";
        string tagName = "testt";
        string measureName = "testm";

        using var host = await FakeHost.CreateBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services
                        .AddRouting()
                        .AddLatencyContext()
                        .AddRequestLatencyTelemetry()
                        .RegisterCheckpointNames(new[] { checkpointName })
                        .RegisterTagNames(new[] { tagName })
                        .RegisterMeasureNames(new[] { measureName });
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseRequestLatencyTelemetry();
                    app.Use(async (context, next) =>
                    {
                        string tagValue = "testVal";
                        int measureValue = 17;
                        int taskTimeMs = 20;

                        isInLambda = true;
                        var latencyContext = context.RequestServices.GetRequiredService<ILatencyContext>();
                        var tokenIssuer = context.RequestServices.GetRequiredService<ILatencyContextTokenIssuer>();
                        Assert.NotNull(latencyContext);
                        latencyContext.AddCheckpoint(tokenIssuer.GetCheckpointToken(checkpointName));
                        latencyContext.SetTag(tokenIssuer.GetTagToken(tagName), tagValue);
                        latencyContext.RecordMeasure(tokenIssuer.GetMeasureToken(measureName), measureValue);
                        await Task.Delay(taskTimeMs + 10); // Adding 10 ms buffer
                        await next.Invoke().ConfigureAwait(false);

                        var ld = latencyContext!.LatencyData;
                        Assert.True(IsMatchByName(ld.Checkpoints, (c) => c.Name == checkpointName));
                        Assert.True(IsMatchByName(ld.Measures, (m) => m.Name == measureName));
                        Assert.True(IsMatchByName(ld.Tags, (t) => t.Name == tagName));
                        Assert.True(((double)ld.DurationTimestamp / ld.DurationTimestampFrequency) * 1000 >= taskTimeMs);
                    });

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/", async context =>
                        {
                            await context.Response.WriteAsync("Hello World!");
                        });
                    });
                }))
            .StartAsync().ConfigureAwait(false);

        _ = await host.GetTestClient().GetAsync("/").ConfigureAwait(false);
        await host.StopAsync();

        Assert.True(isInLambda);
    }

    private static bool IsMatchByName<TX>(in ReadOnlySpan<TX> span, Func<TX, bool> isMatch)
    {
        for (int i = 0; i < span.Length; i++)
        {
            if (isMatch(span[i]))
            {
                return true;
            }
        }

        return false;
    }
}
