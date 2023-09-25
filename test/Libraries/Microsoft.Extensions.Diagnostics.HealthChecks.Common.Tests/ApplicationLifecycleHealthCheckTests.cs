// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Test;

public class ApplicationLifecycleHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_AssertHealthStatusAfterApplicationLifecycleEvents()
    {
        using MockHostApplicationLifetime mockHostApplicationLifetime = new MockHostApplicationLifetime();
        ApplicationLifecycleHealthCheck healthCheck = new ApplicationLifecycleHealthCheck(mockHostApplicationLifetime);
        HealthCheckContext context = new HealthCheckContext();

        Assert.Equal(HealthStatus.Unhealthy, (await healthCheck.CheckHealthAsync(context, CancellationToken.None)).Status);

        mockHostApplicationLifetime.StartApplication();
        Assert.Equal(HealthStatus.Healthy, (await healthCheck.CheckHealthAsync(context, CancellationToken.None)).Status);

        mockHostApplicationLifetime.StoppingApplication();
        Assert.Equal(HealthStatus.Unhealthy, (await healthCheck.CheckHealthAsync(context, CancellationToken.None)).Status);

        mockHostApplicationLifetime.StopApplication();
        Assert.Equal(HealthStatus.Unhealthy, (await healthCheck.CheckHealthAsync(context, CancellationToken.None)).Status);
    }
}
