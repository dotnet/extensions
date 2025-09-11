// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

public class ResourceQuotas
{
    /// <summary>
    /// Gets or sets the resource memory limit the container is allowed to use.
    /// </summary>
    public ulong LimitsMemory { get; set; }

    /// <summary>
    /// Gets or sets the resource CPU limit the container is allowed to use.
    /// </summary>
    public ulong LimitsCpu { get; set; }

    /// <summary>
    /// Gets or sets the resource memory request the container is allowed to use.
    /// </summary>
    public ulong RequestsMemory { get; set; }

    /// <summary>
    /// Gets or sets the resource CPU request the container is allowed to use.
    /// </summary>
    public ulong RequestsCpu { get; set; }
}
