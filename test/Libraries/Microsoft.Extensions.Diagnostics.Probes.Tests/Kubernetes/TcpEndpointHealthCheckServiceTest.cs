// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.Probes.Tests;

[CollectionDefinition(nameof(TcpEndpointHealthCheckServiceTest), DisableParallelization = true)]
public class TcpEndpointHealthCheckServiceTest
{
    [Fact]
    public async Task ExecuteAsync_CheckListenerOpenAndCloseAfterHealthStatusEvents()
    {
        var port = GetFreeTcpPort();

        using var cts = new CancellationTokenSource();

        var healthCheckService = new MockHealthCheckService();

        var options = new KubernetesProbesOptions.EndpointOptions
        {
            TcpPort = port,
        };
        var timeProvider = new FakeTimeProvider();
        using var tcpEndpointHealthCheckService = new TcpEndpointHealthCheckService(
            new FakeLogger<TcpEndpointHealthCheckService>(),
            healthCheckService,
            options)
        {
            TimeProvider = timeProvider
        };

        Assert.False(IsTcpOpened(port));

        await tcpEndpointHealthCheckService.StartAsync(cts.Token);
        await tcpEndpointHealthCheckService.UpdateHealthStatusAsync(cts.Token);

        Assert.True(IsTcpOpened(port));

        timeProvider.Advance(TimeSpan.FromMinutes(1));

        Assert.True(IsTcpOpened(port));

        healthCheckService.IsHealthy = false;
        await tcpEndpointHealthCheckService.UpdateHealthStatusAsync(cts.Token);

        Assert.False(IsTcpOpened(port));

        timeProvider.Advance(TimeSpan.FromMinutes(1));

        Assert.False(IsTcpOpened(port));

        healthCheckService.IsHealthy = true;
        await tcpEndpointHealthCheckService.UpdateHealthStatusAsync(cts.Token);

        Assert.True(IsTcpOpened(port));

        cts.Cancel();
    }

#if NET5_0_OR_GREATER
    [Fact]
    public async Task ExecuteAsync_Does_Nothing_On_Cancellation()
    {
        var port = GetFreeTcpPort();

        var healthCheckService = new MockHealthCheckService();

        var options = new KubernetesProbesOptions.EndpointOptions
        {
            TcpPort = port,
        };
        var timeProvider = new FakeTimeProvider();
        using var tcpEndpointHealthCheckService = new TcpEndpointHealthCheckService(
            new FakeLogger<TcpEndpointHealthCheckService>(),
            healthCheckService,
            options)
        {
            TimeProvider = timeProvider
        };

        using var cts = new CancellationTokenSource();

        cts.Cancel();
        await tcpEndpointHealthCheckService.StartAsync(cts.Token);

        Assert.False(IsTcpOpened(port));
    }
#endif

    private static bool IsTcpOpened(int port)
    {
        try
        {
            using TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(new IPEndPoint(IPAddress.Loopback, port));
            tcpClient.Close();
            return true;
        }
        catch (SocketException e)
        {
            if (e.SocketErrorCode == SocketError.ConnectionRefused)
            {
                return false;
            }
            else
            {
                throw;
            }
        }
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
