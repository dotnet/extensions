// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Collections.Generic;

namespace Microsoft.Extensions.Http.Telemetry;

/// <summary>
/// Interface for passing dependency metadata.
/// </summary>
public interface IDownstreamDependencyMetadata
{
    /// <summary>
    /// Gets the name of the dependent service.
    /// </summary>
    string DependencyName { get; }

    /// <summary>
    /// Gets the list of host name suffixes that can uniquely identify a host as this dependency.
    /// </summary>
    ISet<string> UniqueHostNameSuffixes { get; }

    /// <summary>
    /// Gets the list of all metadata for all routes to the dependency service.
    /// </summary>
    ISet<RequestMetadata> RequestMetadata { get; }
}
