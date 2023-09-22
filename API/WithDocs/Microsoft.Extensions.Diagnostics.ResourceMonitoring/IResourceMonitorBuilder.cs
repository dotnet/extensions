// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Helps building the resource monitoring infrastructure.
/// </summary>
public interface IResourceMonitorBuilder
{
    /// <summary>
    /// Gets the service collection being manipulated by the builder.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Adds a resource utilization publisher that gets invoked whenever resource utilization is computed.
    /// </summary>
    /// <typeparam name="T">The publisher's implementation type.</typeparam>
    /// <returns>The value of the object instance.</returns>
    IResourceMonitorBuilder AddPublisher<T>() where T : class, IResourceUtilizationPublisher;
}
