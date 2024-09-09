// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

internal class DummyLinuxUtilizationParser : ILinuxUtilizationParser
{
    public ulong GetAvailableMemoryInBytes() => 1;
    public long GetCgroupCpuUsageInNanoseconds() => 0;
    public float GetCgroupLimitedCpus() => 1;
    public float GetCgroupRequestCpu() => 1;
    public ulong GetHostAvailableMemory() => 0;
    public float GetHostCpuCount() => 1;
    public long GetHostCpuUsageInNanoseconds() => 0;
    public ulong GetMemoryUsageInBytes() => 0;
}