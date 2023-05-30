// Assembly 'Microsoft.Extensions.Telemetry'

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Enrichment;

public static class ServiceEnricherDimensions
{
    public const string ApplicationName = "env_app_name";
    public const string EnvironmentName = "env_cloud_env";
    public const string DeploymentRing = "env_cloud_deploymentRing";
    public const string BuildVersion = "env_cloud_roleVer";
    public static IReadOnlyList<string> DimensionNames { get; }
}
