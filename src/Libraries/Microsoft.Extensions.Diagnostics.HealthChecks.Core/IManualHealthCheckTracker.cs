// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// A helper to track all instances of IManualHealthCheck registered in the application.
/// </summary>
internal interface IManualHealthCheckTracker
{
    /// <summary>
    /// Registers a new IManualHealthCheck into the tracker.
    /// </summary>
    /// <param name="check">The manual health check to be added.</param>
    void Register(IManualHealthCheck check);

    /// <summary>
    /// Removes a IManualHealthCheck from the tracker.
    /// </summary>
    /// <param name="checkToRemove">The manual health check to be removed.</param>
    void Unregister(IManualHealthCheck checkToRemove);

    /// <summary>
    /// Gets the HealthCheckResult generated from the registered list of IManualHealthCheck.
    /// </summary>
    /// <returns>
    /// A <see cref="Task{T}" /> containing the HealthCheckResult generated.
    /// </returns>
    HealthCheckResult GetHealthCheckResult();
}
