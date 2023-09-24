// Assembly 'Microsoft.Extensions.Diagnostics.Extra'

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.Enrichment;

/// <summary>
/// Constants used for enrichment tags.
/// </summary>
public static class ServiceEnricherTags
{
    /// <summary>
    /// Application name.
    /// </summary>
    public const string ApplicationName = "env_app_name";

    /// <summary>
    /// Environment name.
    /// </summary>
    public const string EnvironmentName = "env_cloud_env";

    /// <summary>
    /// Deployment ring.
    /// </summary>
    public const string DeploymentRing = "env_cloud_deploymentRing";

    /// <summary>
    /// Build version.
    /// </summary>
    public const string BuildVersion = "env_cloud_roleVer";

    /// <summary>
    /// Gets a list of all dimension names.
    /// </summary>
    /// <returns>A read-only <see cref="T:System.Collections.Generic.IReadOnlyList`1" /> of all dimension names.</returns>
    public static IReadOnlyList<string> DimensionNames { get; }
}
