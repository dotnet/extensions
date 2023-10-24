﻿// Licensed to the .NET Foundation under one or more agreements.
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
    // See https://github.com/open-telemetry/semantic-conventions/blob/main/docs/resource/README.md#service.
    public const string ApplicationName = "service.name";

    /// <summary>
    /// Environment name.
    /// </summary>
    // See https://github.com/open-telemetry/semantic-conventions/blob/main/docs/resource/deployment-environment.md#deployment.
    public const string EnvironmentName = "deployment.environment";

    /// <summary>
    /// Deployment ring.
    /// </summary>
    public const string DeploymentRing = "DeploymentRing";

    /// <summary>
    /// Build version.
    /// </summary>
    // See https://github.com/open-telemetry/semantic-conventions/blob/main/docs/resource/README.md#service.
    public const string BuildVersion = "service.version";

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
