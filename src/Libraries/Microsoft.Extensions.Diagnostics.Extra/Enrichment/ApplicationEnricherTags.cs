// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.Enrichment;

/// <summary>
/// Constants used for enrichment tags.
/// </summary>
public static class ApplicationEnricherTags
{
    /// <summary>
    /// Application name.
    /// </summary>
    public const string ApplicationName = "AppName";

    /// <summary>
    /// Environment name.
    /// </summary>
    public const string EnvironmentName = "CloudEnv";

    /// <summary>
    /// Deployment ring.
    /// </summary>
    public const string DeploymentRing = "CloudDeploymentRing";

    /// <summary>
    /// Build version.
    /// </summary>
    public const string BuildVersion = "CloudRoleVer";

    /// <summary>
    /// Gets a list of all dimension names.
    /// </summary>
    /// <returns>A read-only <see cref="IReadOnlyList{String}"/> of all dimension names.</returns>
    public static IReadOnlyList<string> DimensionNames { get; } =
        Array.AsReadOnly(new[]
        {
            ApplicationName,
            EnvironmentName,
            BuildVersion,
            DeploymentRing
        });
}
