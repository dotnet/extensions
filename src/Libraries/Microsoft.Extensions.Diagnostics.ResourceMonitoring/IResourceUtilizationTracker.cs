// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Provides the ability to sample the system for current resource utilization.
/// </summary>
public interface IResourceUtilizationTracker
{
    /// <summary>
    /// Gets utilization for the specified time window.
    /// </summary>
    /// <param name="window">A <see cref="TimeSpan"/> representing the time window for which utilization is requested.</param>
    /// <returns>The utilization during the time window specified by <paramref name="window"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="window"/> is greater than the maximum window size configured while adding the service to the services collection.
    /// </exception>
    Utilization GetUtilization(TimeSpan window);
}
