// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Health check which considers the application healthy after it is reported as started by <see cref="IHostApplicationLifetime" />
/// and unhealthy when it is shutting down.
/// </summary>
internal sealed class ApplicationLifecycleHealthCheck : IHealthCheck
{
    private static readonly Task<HealthCheckResult> _healthy = Task.FromResult(HealthCheckResult.Healthy());
    private static readonly Task<HealthCheckResult> _unhealthyNotStarted = Task.FromResult(HealthCheckResult.Unhealthy("Not Started"));
    private static readonly Task<HealthCheckResult> _unhealthyStopping = Task.FromResult(HealthCheckResult.Unhealthy("Stopping"));
    private static readonly Task<HealthCheckResult> _unhealthyStopped = Task.FromResult(HealthCheckResult.Unhealthy("Stopped"));
    private readonly IHostApplicationLifetime _appLifetime;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationLifecycleHealthCheck"/> class.
    /// </summary>
    /// <param name="appLifetime">Reference to application lifetime.</param>
    public ApplicationLifecycleHealthCheck(IHostApplicationLifetime appLifetime)
    {
        _appLifetime = appLifetime;
    }

    /// <summary>
    /// Runs the health check, returning the status of the component being checked.
    /// </summary>
    /// <remarks>
    /// This method is called from <see href="https://github.com/dotnet/aspnetcore/blob/main/src/HealthChecks/HealthChecks/src/HealthCheckPublisherHostedService.cs"/>
    /// with period and other settings defined in <see cref="HealthCheckPublisherOptions"/>.
    /// </remarks>
    /// <param name="context">A context object associated with the current execution.</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken that can be used to cancel the health check.</param>
    /// <returns>
    /// A <see cref="Task{T}" /> that completes when the health check has finished,
    /// yielding the status of the component being checked.
    /// </returns>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        bool isStarted = _appLifetime.ApplicationStarted.IsCancellationRequested;
        if (!isStarted)
        {
            return _unhealthyNotStarted;
        }

        bool isStopping = _appLifetime.ApplicationStopping.IsCancellationRequested;
        if (isStopping)
        {
            return _unhealthyStopping;
        }

        bool isStopped = _appLifetime.ApplicationStopped.IsCancellationRequested;
        if (isStopped)
        {
            return _unhealthyStopped;
        }

        return _healthy;
    }
}
