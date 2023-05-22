// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

internal sealed class NullResourceUtilizationTrackerService : IResourceUtilizationTracker
{
    private const double CpuUnits = 1.0;
    private static readonly Utilization _utilization = new(0.0, 0U, new(CpuUnits, CpuUnits, long.MaxValue, long.MaxValue));

    public Utilization GetUtilization(TimeSpan aggregationPeriod) => _utilization;
}
