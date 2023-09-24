// Assembly 'Microsoft.Extensions.Diagnostics.Extra'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.Enrichment;

public class ServiceLogEnricherOptions
{
    public bool EnvironmentName { get; set; }
    public bool ApplicationName { get; set; }
    public bool DeploymentRing { get; set; }
    public bool BuildVersion { get; set; }
    public ServiceLogEnricherOptions();
}
