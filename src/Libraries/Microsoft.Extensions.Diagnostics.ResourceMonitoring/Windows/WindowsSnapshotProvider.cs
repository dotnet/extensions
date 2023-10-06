// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

internal sealed class WindowsSnapshotProvider : ISnapshotProvider
{
    public SystemResources Resources { get; }

    internal TimeProvider TimeProvider = TimeProvider.System;

    public WindowsSnapshotProvider(ILogger<WindowsSnapshotProvider> logger)
    {
        Log.RunningOutsideJobObject(logger);

        var memoryStatus = new MemoryInfo().GetMemoryStatus();
        var cpuUnits = Environment.ProcessorCount;
        Resources = new SystemResources(cpuUnits, cpuUnits, memoryStatus.TotalPhys, memoryStatus.TotalPhys);
    }

    public Snapshot GetSnapshot()
    {
        // Gather the information
        // Get CPU kernel and user ticks
        var process = Process.GetCurrentProcess();

        return new Snapshot(
            TimeSpan.FromTicks(TimeProvider.GetUtcNow().Ticks),
            TimeSpan.FromTicks(process.PrivilegedProcessorTime.Ticks),
            TimeSpan.FromTicks(process.UserProcessorTime.Ticks),
            (ulong)process.VirtualMemorySize64);
    }
}
