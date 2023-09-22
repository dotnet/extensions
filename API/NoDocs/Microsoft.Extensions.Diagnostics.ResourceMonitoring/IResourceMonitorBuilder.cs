// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

public interface IResourceMonitorBuilder
{
    IServiceCollection Services { get; }
    IResourceMonitorBuilder AddPublisher<T>() where T : class, IResourceUtilizationPublisher;
}
