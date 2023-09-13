﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

internal readonly struct WindowsPerfCounters
{
    public PerformanceCounter CpuUtilization { get; }
    public PerformanceCounter MemUtilization { get; }
    public PerformanceCounter MemLimit { get; }

    public WindowsPerfCounters(
        PerformanceCounter cpuUtilization,
        PerformanceCounter memUtilization,
        PerformanceCounter memLimit)
    {
        CpuUtilization = cpuUtilization;
        MemUtilization = memUtilization;
        MemLimit = memLimit;
    }
}