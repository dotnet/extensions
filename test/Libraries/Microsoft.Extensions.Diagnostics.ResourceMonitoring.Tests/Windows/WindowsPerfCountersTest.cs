// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Test;

public sealed class WindowsPerfCountersTest
{
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Windows-specific code.")]
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Indeed, this Windows-specific")]
    public void Basic()
    {
        using var cpuUtilization = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        using var memUtilization = new PerformanceCounter("Memory", "% Committed Bytes In Use");
        using var memLimit = new PerformanceCounter("Memory", "Committed Bytes");

        var counters = new WindowsPerfCounters(cpuUtilization, memUtilization, memLimit);

        Assert.Equal(cpuUtilization, counters.CpuUtilization);
        Assert.Equal(memUtilization, counters.MemUtilization);
        Assert.Equal(memLimit, counters.MemLimit);
    }
}
#endif
