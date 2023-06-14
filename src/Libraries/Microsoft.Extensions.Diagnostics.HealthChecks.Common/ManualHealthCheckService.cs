// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Lets you manually modify the healthiness of your application. This health check will only report healthy on all registered instances of <see cref="IManualHealthCheck"/> are healthy.
/// </summary>
/// <remarks>
/// This health check should be used when you want to have the flexibility to claim your service as unhealthy.
/// </remarks>
internal sealed class ManualHealthCheckService : IHealthCheck
{
    private readonly ManualHealthCheckTracker _tracker;

    public ManualHealthCheckService(ManualHealthCheckTracker tracker)
    {
        _tracker = tracker;
    }

    /// <summary>
    /// Runs the health check, returning the status of the component being checked.
    /// </summary>
    /// <remarks>
    /// This method is called from <see href="https://github.com/dotnet/aspnetcore/blob/main/src/HealthChecks/HealthChecks/src/HealthCheckPublisherHostedService.cs"/>
    /// with period and other settings defined in <see cref="HealthCheckPublisherOptions"/>.
    /// </remarks>
    /// <param name="context">A context object associated with the current execution.</param>
    /// <param name="cancellationToken">Not used in the current implementation.</param>
    /// <returns>
    /// A <see cref="Task{T}" /> that completes when the health check has finished,
    /// yielding the status of the component being checked.
    /// </returns>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) => Task.FromResult(_tracker.GetHealthCheckResult());
}
