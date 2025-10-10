// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Represents resource quota information for a container, including CPU and memory limits and requests.
/// Limits define the maximum resources a container can use, while requests specify the minimum guaranteed resources.
/// </summary>
public class ResourceQuota
{
    /// <summary>
    /// Gets or sets the resource memory limit the container is allowed to use.
    /// </summary>
    public ulong LimitsMemory { get; set; }

    /// <summary>
    /// Gets or sets the resource CPU limit the container is allowed to use.
    /// </summary>
    public double LimitsCpu { get; set; }

    /// <summary>
    /// Gets or sets the resource memory request the container is allowed to use.
    /// </summary>
    public ulong RequestsMemory { get; set; }

    /// <summary>
    /// Gets or sets the resource CPU request the container is allowed to use.
    /// </summary>
    public double RequestsCpu { get; set; }
}
