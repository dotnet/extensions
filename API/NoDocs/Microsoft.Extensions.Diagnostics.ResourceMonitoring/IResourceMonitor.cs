// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

public interface IResourceMonitor
{
    ResourceUtilization GetUtilization(TimeSpan window);
}
