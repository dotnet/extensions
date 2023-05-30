// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Provides the ability to sample the system for current resource utilization.
/// </summary>
public interface IResourceMonitor
{
    /// <summary>
    /// Gets utilization for the specified time window.
    /// </summary>
    /// <param name="window">A <see cref="T:System.TimeSpan" /> representing the time window for which utilization is requested.</param>
    /// <returns>The utilization during the time window specified by <paramref name="window" />.</returns>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// Thrown when <paramref name="window" /> is greater than the maximum window size configured while adding the service to the services collection.
    /// </exception>
    Utilization GetUtilization(TimeSpan window);
}
