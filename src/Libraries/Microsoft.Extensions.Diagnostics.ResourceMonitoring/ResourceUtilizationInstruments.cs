// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Represents the names of instruments published by this package.
/// </summary>
/// <remarks>
/// These metrics are currently only published on Linux.
/// </remarks>
/// <seealso cref="System.Diagnostics.Metrics.Instrument"/>
internal static class ResourceUtilizationInstruments
{
    /// <summary>
    /// Gets the CPU consumption of the running process in range <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// The type of an instrument is <see cref="System.Diagnostics.Metrics.ObservableGauge{T}"/>.
    /// </remarks>
    public const string ProcessCpuUtilization = "process.cpu.utilization";

    /// <summary>
    /// Gets the CPU consumption of all processes running inside a container or control group in range <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// The type of an instrument is <see cref="System.Diagnostics.Metrics.ObservableGauge{T}"/>.
    /// </remarks>
    public const string ContainerCpuUtilization = "container.cpu.utilization";

    /// <summary>
    /// Gets the memory consumption of the running process in range <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// The type of an instrument is <see cref="System.Diagnostics.Metrics.ObservableGauge{T}"/>.
    /// </remarks>
    public const string ProcessMemoryUtilization = "process.memory.utilization";

    /// <summary>
    /// Gets the memory consumption of all processes running inside a container or control group in range <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// The type of an instrument is <see cref="System.Diagnostics.Metrics.ObservableGauge{T}"/>.
    /// </remarks>
    public const string ContainerMemoryUtilization = "container.memory.utilization";
}
