// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

public interface IResourceMonitor
{
    Utilization GetUtilization(TimeSpan window);
}
