// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Represents resource quota information for a process or container, including CPU and memory constraints.
/// Maximum values define the upper limits of resource usage, while guaranteed values specify 
/// the minimum assured resource allocations.
/// </summary>
public sealed class ResourceQuota
{
    /// <summary>
    /// Gets or sets the maximum memory that can be used in bytes.
    /// </summary>
    public ulong MaxMemoryInBytes { get; set; }

    /// <summary>
    /// Gets or sets the maximum CPU that can be used in cores.
    /// </summary>
    public double MaxCpuInCores { get; set; }

    /// <summary>
    /// Gets or sets the guaranteed (minimum) memory allocation in bytes.
    /// </summary>
    public ulong GuaranteedMemoryInBytes { get; set; }

    /// <summary>
    /// Gets or sets the guaranteed (minimum) CPU allocation in cores.
    /// </summary>
    public double GuaranteedCpuInCores { get; set; }
}
