// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Connections.Test;

[Collection(nameof(StaticFakeClockExecution))]
public sealed class AcceptanceTest
{
    private readonly FakeTimeProvider _fakeTimeProvider;

    public AcceptanceTest()
    {
        _fakeTimeProvider = new FakeTimeProvider();
    }

    [Fact]
    public async Task ConnectionTimeout_KeepsConnection_BeforeTimeout()
    {
        var shutdownTimeout = TimeSpan.FromMinutes(3);
        using var host = await FakeHost.CreateBuilder()
            .ConfigureWebHost(webHostBuilder => webHostBuilder
                .ConfigureServices(services =>
                {
                    services.Configure<ConnectionTimeoutOptions>(options => options.Timeout = shutdownTimeout);
                })
                .ListenHttpOnAnyPort()
                .UseKestrel((_, serverOptions) =>
                {
                    serverOptions.ConfigureEndpointDefaults(listenOptions =>
                    {
                        listenOptions.UseConnectionTimeout();
                    });
                })
                .UseStartup<Startup>())
            .StartAsync();

        var address = host.GetListenUris().Single();

        using var httpClientHandler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 1
        };

        string? initialConnectionId;
        using var httpClient = new HttpClient(httpClientHandler);
        using (var response = await httpClient.GetAsync(address))
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            initialConnectionId = response.Headers.GetValues("ConnectionId").FirstOrDefault();
            initialConnectionId.Should().NotBeNullOrEmpty();
        }

        _fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(16));

        using (var response = await httpClient.GetAsync(address))
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var connectionId = response.Headers.GetValues("ConnectionId").FirstOrDefault();
            connectionId.Should().NotBeNullOrEmpty();
            connectionId.Should().Be(initialConnectionId);
        }
    }
}
