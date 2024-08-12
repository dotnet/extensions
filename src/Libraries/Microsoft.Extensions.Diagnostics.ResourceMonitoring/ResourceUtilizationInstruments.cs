﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

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
    /// The name of an instrument to retrieve CPU limit consumption of all processes running inside a container or control group in range <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// The type of an instrument is <see cref="System.Diagnostics.Metrics.ObservableGauge{T}"/>.
    /// </remarks>
    public const string ContainerCpuLimitUtilization = "container.cpu.limit.utilization";

    /// <summary>
    /// The name of an instrument to retrieve CPU request consumption of all processes running inside a container or control group in range <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// The type of an instrument is <see cref="System.Diagnostics.Metrics.ObservableGauge{T}"/>.
    /// </remarks>
    public const string ContainerCpuRequestUtilization = "container.cpu.request.utilization";

    /// <summary>
    /// The name of an instrument to retrieve memory limit consumption of all processes running inside a container or control group in range <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// The type of an instrument is <see cref="System.Diagnostics.Metrics.ObservableGauge{T}"/>.
    /// </remarks>
    public const string ContainerMemoryLimitUtilization = "container.memory.limit.utilization";

    /// <summary>
    /// The name of an instrument to retrieve CPU consumption share of the running process in range <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// The type of an instrument is <see cref="System.Diagnostics.Metrics.ObservableGauge{T}"/>.
    /// </remarks>
    public const string ProcessCpuUtilization = "process.cpu.utilization";

    /// <summary>
    /// The name of an instrument to retrieve memory consumption share of the running process in range <c>[0, 1]</c>.
    /// </summary>
    /// <remarks>
    /// The type of an instrument is <see cref="System.Diagnostics.Metrics.ObservableGauge{T}"/>.
    /// </remarks>
    public const string ProcessMemoryUtilization = "dotnet.process.memory.virtual.utilization";
}
