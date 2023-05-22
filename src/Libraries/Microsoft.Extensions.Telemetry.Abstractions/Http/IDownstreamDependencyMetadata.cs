// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public string DependencyName { get; }

    /// <summary>
    /// Gets the list of host name suffixes that can uniquely identify a host as this dependency.
    /// </summary>
    public ISet<string> UniqueHostNameSuffixes { get; }

    /// <summary>
    /// Gets the list of all metadata for all routes to the dependency service.
    /// </summary>
    public ISet<RequestMetadata> RequestMetadata { get; }
}
