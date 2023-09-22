// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Defines the contract for a resource utilization publisher that gets invoked whenever resource utilization is computed.
/// </summary>
public interface IResourceUtilizationPublisher
{
    /// <summary>
    /// This method is invoked by the monitoring infrastructure whenever resource utilization is computed.
    /// </summary>
    /// <param name="utilization">A snapshot of the system's current resource utilization.</param>
    /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> used to cancel the publish operation.</param>
    /// <returns>A value task to track the publication operation.</returns>
    ValueTask PublishAsync(ResourceUtilization utilization, CancellationToken cancellationToken);
}
