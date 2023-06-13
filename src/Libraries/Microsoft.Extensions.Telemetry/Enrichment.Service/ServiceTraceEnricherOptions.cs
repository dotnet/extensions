// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AmbientMetadata;

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// Options for the service trace enricher.
/// </summary>
public class ServiceTraceEnricherOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ApplicationMetadata.EnvironmentName"/> is used for trace enrichment.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool EnvironmentName { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ApplicationMetadata.ApplicationName"/> is used for trace enrichment.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool ApplicationName { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ApplicationMetadata.DeploymentRing"/> is used for trace enrichment.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false" />.
    /// </value>
    public bool DeploymentRing { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ApplicationMetadata.BuildVersion"/> is used for trace enrichment.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false" />.
    /// </value>
    public bool BuildVersion { get; set; }
}
