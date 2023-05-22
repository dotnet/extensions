// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Core.Tests;

public class KubernetesHealthCheckPublisherTest
{
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux)]
    public async Task PublishAsync_CheckIfTcpPortIsOpenedAfterHealthStatusEvents()
    {
        KubernetesHealthCheckPublisherOptions options = new KubernetesHealthCheckPublisherOptions();
        KubernetesHealthCheckPublisher publisher = new KubernetesHealthCheckPublisher(Microsoft.Extensions.Options.Options.Create(options));

        Assert.False(IsTcpPortOpened());

        await publisher.PublishAsync(CreateHealthReport(HealthStatus.Healthy), CancellationToken.None);
        Assert.True(IsTcpPortOpened());

        await publisher.PublishAsync(CreateHealthReport(HealthStatus.Healthy), CancellationToken.None);
        Assert.True(IsTcpPortOpened());

        await publisher.PublishAsync(CreateHealthReport(HealthStatus.Unhealthy), CancellationToken.None);
        Assert.False(IsTcpPortOpened());

        await publisher.PublishAsync(CreateHealthReport(HealthStatus.Unhealthy), CancellationToken.None);
        Assert.False(IsTcpPortOpened());

        await publisher.PublishAsync(CreateHealthReport(HealthStatus.Healthy), CancellationToken.None);
        Assert.True(IsTcpPortOpened());
    }

    [Fact]
    public void Ctor_ThrowsWhenOptionsValueNull()
    {
        Assert.Throws<ArgumentException>(() => new KubernetesHealthCheckPublisher(Microsoft.Extensions.Options.Options.Create<KubernetesHealthCheckPublisherOptions>(null!)));
    }

    private static HealthReport CreateHealthReport(HealthStatus healthStatus)
    {
        HealthReportEntry entry = new HealthReportEntry(healthStatus, null, TimeSpan.Zero, null, null);
        var healthStatusRecords = new Dictionary<string, HealthReportEntry> { { "id", entry } };
        return new HealthReport(healthStatusRecords, TimeSpan.Zero);
    }

    private static bool IsTcpPortOpened()
    {
        try
        {
            using TcpClient tcpClient = new TcpClient("localhost", 2305);
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
}
