// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Probes;

/// <summary>
/// Standardized health check tags for probes.
/// </summary>
public static class ProbeTags
{
    /// <summary>
    /// Liveness probe tag.
    /// </summary>
    public const string Liveness = "_probe_liveness";

    /// <summary>
    /// Startup probe tag.
    /// </summary>
    public const string Startup = "_probe_startup";

    /// <summary>
    /// Readiness probe tag.
    /// </summary>
    public const string Readiness = "_probe_readiness";
}
