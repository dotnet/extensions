// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;

internal static class FaultInjectionTelemetryHandler
{
    private const string NotApplicable = "N/A";

    public static void LogAndMeter(
        ILogger logger, HttpClientFaultInjectionMetricCounter counter, string groupName, string faultType, string injectedValue, string? httpContentKey)
    {
        httpContentKey ??= NotApplicable;

        Log.LogInjection(logger, groupName, faultType, injectedValue, httpContentKey);
        counter.Add(1, groupName, faultType, injectedValue, httpContentKey);
    }
}
