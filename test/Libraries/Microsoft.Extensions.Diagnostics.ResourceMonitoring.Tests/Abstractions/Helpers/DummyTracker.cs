// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test.Helpers;

internal class DummyTracker : IResourceMonitor
{
    public const double CpuPercentage = 50.0;
    public const double MemoryPercentage = 10.0;
    public const ulong MemoryUsed = 100;
    public const ulong MemoryTotal = 1000;
    public const uint CpuUnits = 1;

    public Utilization GetUtilization(TimeSpan aggregationPeriod) => new(CpuPercentage, MemoryUsed, new SystemResources(CpuUnits, CpuUnits, MemoryTotal, MemoryTotal));
}
