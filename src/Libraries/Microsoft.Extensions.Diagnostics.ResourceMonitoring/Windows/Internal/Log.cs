// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

#pragma warning disable S109
#pragma warning disable IDE0060 // Remove unused parameters - Reason: used by source generator.

internal static partial class Log
{
    [LogMethod(1, LogLevel.Error, "Windows performance counter `{counterName}` does not exist.")]
    public static partial void CounterDoesNotExist(ILogger logger, string counterName);

    [LogMethod(2, LogLevel.Information, "Resource Utilization is running inside a Job Object. For more information about Job Objects see https://aka.ms/job-objects")]
    public static partial void RunningInsideJobObject(ILogger logger);

    [LogMethod(3, LogLevel.Information, "Resource Utilization is running outside of Job Object. For more information about Job Objects see https://aka.ms/job-objects")]
    public static partial void RunningOutsideJobObject(ILogger logger);
}
