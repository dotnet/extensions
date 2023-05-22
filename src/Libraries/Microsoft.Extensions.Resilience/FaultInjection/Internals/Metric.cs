// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Telemetry.Metering;

namespace Microsoft.Extensions.Resilience.FaultInjection;

internal static partial class Metric
{
    [Counter(
        FaultInjectionEventMeterDimensions.FaultInjectionGroupName,
        FaultInjectionEventMeterDimensions.FaultType,
        FaultInjectionEventMeterDimensions.InjectedValue,
        Name = @"R9\Resilience\FaultInjection\InjectedFaults")]
    public static partial FaultInjectionMetricCounter CreateFaultInjectionMetricCounter(Meter meter);
}
