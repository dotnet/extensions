// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Helps building resource monitoring infra.
/// </summary>
public interface IResourceMonitorBuilder
{
    /// <summary>
    /// Gets the service collection being manipulated by the builder.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Adds implementation of the utilization data publisher.
    /// </summary>
    /// <typeparam name="T">An implementation of <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.IResourceUtilizationPublisher" /> that is used by the tracker to publish <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.Utilization" /> to 3rd parties.</typeparam>
    /// <returns>Instance of <see cref="T:Microsoft.Extensions.Diagnostics.ResourceMonitoring.IResourceMonitorBuilder" /> for further configurations.</returns>
    IResourceMonitorBuilder AddPublisher<T>() where T : class, IResourceUtilizationPublisher;
}
