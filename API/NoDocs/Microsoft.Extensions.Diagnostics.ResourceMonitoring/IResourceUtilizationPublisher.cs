// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

public interface IResourceUtilizationPublisher
{
    ValueTask PublishAsync(Utilization utilization, CancellationToken cancellationToken);
}
