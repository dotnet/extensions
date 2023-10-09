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
    public const string ApplicationName = "App.Name";

    /// <summary>
    /// Environment name.
    /// </summary>
    public const string EnvironmentName = "Cloud.Env";

    /// <summary>
    /// Deployment ring.
    /// </summary>
    public const string DeploymentRing = "Cloud.DeploymentRing";

    /// <summary>
    /// Build version.
    /// </summary>
    public const string BuildVersion = "Cloud.RoleVer";

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
