// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.HealthChecks.Core.Tests;

public class ManualHealthCheckTest
{
    [Fact]
    public async Task CheckHealthAsync_Initial()
    {
        ManualHealthCheckTracker manualHealthCheckTracker = new ManualHealthCheckTracker();
        ManualHealthCheckService manualHealthCheckService = new ManualHealthCheckService(manualHealthCheckTracker);
        HealthCheckContext context = new HealthCheckContext();

        var healthCheckResult = await manualHealthCheckService.CheckHealthAsync(context, CancellationToken.None);
        Assert.Equal(HealthStatus.Healthy, healthCheckResult.Status);
        Assert.Null(healthCheckResult.Description);

        var manualHealthCheck = new ManualHealthCheck<ManualHealthCheckTest>(manualHealthCheckTracker);
        Assert.Equal(HealthStatus.Unhealthy, manualHealthCheck.Result.Status);
        Assert.Equal("Initial state", manualHealthCheck.Result.Description);
        manualHealthCheck.Dispose();
    }

    [Fact]
    public async Task CheckHealthAsync_AssertHealthStatus_AfterChange()
    {
        ManualHealthCheckTracker manualHealthCheckTracker = new ManualHealthCheckTracker();
        ManualHealthCheckService manualHealthCheckService = new ManualHealthCheckService(manualHealthCheckTracker);
        HealthCheckContext context = new HealthCheckContext();
        var manualHealthCheck = new ManualHealthCheck<ManualHealthCheckTest>(manualHealthCheckTracker);

        Assert.Equal(HealthStatus.Unhealthy, (await manualHealthCheckService.CheckHealthAsync(context, CancellationToken.None)).Status);
        Assert.Equal("Initial state", manualHealthCheck.Result.Description);

        manualHealthCheck.ReportUnhealthy("Test reason");
        var healthCheckResultUnhealthy = await manualHealthCheckService.CheckHealthAsync(context, CancellationToken.None);
        Assert.Equal(HealthStatus.Unhealthy, healthCheckResultUnhealthy.Status);
        Assert.Equal("Test reason", healthCheckResultUnhealthy.Description);

        manualHealthCheck.ReportHealthy();
        var healthCheckResultHealthy = await manualHealthCheckService.CheckHealthAsync(context, CancellationToken.None);
        Assert.Equal(HealthStatus.Healthy, healthCheckResultHealthy.Status);
        Assert.Null(healthCheckResultHealthy.Description);
        Assert.Null(manualHealthCheck.Result.Description);
        manualHealthCheck.Dispose();
    }

    [Fact]
    public async Task CheckHealthAsync_AssertHealthStatus_AfterChangeWithReport()
    {
        ManualHealthCheckTracker manualHealthCheckTracker = new ManualHealthCheckTracker();
        ManualHealthCheckService manualHealthCheckService = new ManualHealthCheckService(manualHealthCheckTracker);
        HealthCheckContext context = new HealthCheckContext();
        var manualHealthCheck = new ManualHealthCheck<ManualHealthCheckTest>(manualHealthCheckTracker);

        Assert.Equal(HealthStatus.Unhealthy, (await manualHealthCheckService.CheckHealthAsync(context, CancellationToken.None)).Status);
        Assert.Equal("Initial state", manualHealthCheck.Result.Description);

        var unhealthyCheck = HealthCheckResult.Unhealthy("Test reason");
        manualHealthCheck.Result = unhealthyCheck;
        var healthCheckResultUnhealthy = await manualHealthCheckService.CheckHealthAsync(context, CancellationToken.None);
        Assert.Equal(HealthStatus.Unhealthy, healthCheckResultUnhealthy.Status);
        Assert.Equal("Test reason", healthCheckResultUnhealthy.Description);

        var healthyCheck = HealthCheckResult.Healthy();
        manualHealthCheck.Result = healthyCheck;
        var healthCheckResultHealthy = await manualHealthCheckService.CheckHealthAsync(context, CancellationToken.None);
        Assert.Equal(HealthStatus.Healthy, healthCheckResultHealthy.Status);
        Assert.Null(healthCheckResultHealthy.Description);
        Assert.Null(manualHealthCheck.Result.Description);
        manualHealthCheck.Dispose();
    }

    [Fact]
    public async Task CheckHealthAsync_AssertHealthStatusAfterChange_MultipleModules()
    {
        ManualHealthCheckTracker manualHealthCheckTracker = new ManualHealthCheckTracker();
        ManualHealthCheckService manualHealthCheckService = new ManualHealthCheckService(manualHealthCheckTracker);
        HealthCheckContext context = new HealthCheckContext();
        var manualHealthCheck1 = new ManualHealthCheck<ManualHealthCheckTest>(manualHealthCheckTracker);
        var manualHealthCheck2 = new ManualHealthCheck<ManualHealthCheckExtensionsTest>(manualHealthCheckTracker);

        Assert.Equal(HealthStatus.Unhealthy, (await manualHealthCheckService.CheckHealthAsync(context, CancellationToken.None)).Status);
        Assert.Equal("Initial state", manualHealthCheck1.Result.Description);
        Assert.Equal("Initial state", manualHealthCheck2.Result.Description);

        // Both unhealthy
        manualHealthCheck1.ReportUnhealthy("Test reason 1");
        manualHealthCheck2.ReportUnhealthy("Test reason 2");
        var healthCheckResult2Unhealthy = await manualHealthCheckService.CheckHealthAsync(context, CancellationToken.None);
        Assert.Equal(HealthStatus.Unhealthy, healthCheckResult2Unhealthy.Status);
        Assert.True(healthCheckResult2Unhealthy.Description!.Equals("Test reason 1, Test reason 2")
            || healthCheckResult2Unhealthy.Description.Equals("Test reason 2, Test reason 1"));

        // One unhealthy
        manualHealthCheck1.ReportHealthy();
        var healthCheckResult1Unhealthy = await manualHealthCheckService.CheckHealthAsync(context, CancellationToken.None);
        Assert.Equal(HealthStatus.Unhealthy, healthCheckResult1Unhealthy.Status);
        Assert.Equal("Test reason 2", healthCheckResult1Unhealthy.Description);
        Assert.Null(manualHealthCheck1.Result.Description);

        // Both healthy
        manualHealthCheck2.ReportHealthy();
        var healthCheckResultHealthy = await manualHealthCheckService.CheckHealthAsync(context, CancellationToken.None);
        Assert.Equal(HealthStatus.Healthy, healthCheckResultHealthy.Status);
        Assert.Null(healthCheckResultHealthy.Description);
        Assert.Null(manualHealthCheck1.Result.Description);
        Assert.Null(manualHealthCheck2.Result.Description);

        manualHealthCheck1.Dispose();
        manualHealthCheck2.Dispose();
    }
}
