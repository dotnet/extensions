// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Resilience.FaultInjection;

internal static class FaultInjectionTelemetryHandler
{
    public static void LogAndMeter(
        ILogger logger, FaultInjectionMetricCounter counter, string groupName, string faultType, string injectedValue)
    {
        Log.LogInjection(logger, groupName, faultType, injectedValue);
        counter.Add(1, groupName, faultType, injectedValue);
    }
}
