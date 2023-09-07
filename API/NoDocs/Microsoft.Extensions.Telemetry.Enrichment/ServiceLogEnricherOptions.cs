// Assembly 'Microsoft.Extensions.Telemetry'

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Enrichment;

public class ServiceLogEnricherOptions
{
    public bool EnvironmentName { get; set; }
    public bool ApplicationName { get; set; }
    public bool DeploymentRing { get; set; }
    public bool BuildVersion { get; set; }
    public ServiceLogEnricherOptions();
}
