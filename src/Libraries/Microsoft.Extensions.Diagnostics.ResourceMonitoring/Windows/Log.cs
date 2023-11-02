// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows;

#pragma warning disable S109
#pragma warning disable IDE0060 // Remove unused parameters - Reason: used by source generator.

internal static partial class Log
{
    [LoggerMessage(1, LogLevel.Information, "Resource Monitoring is running inside a Job Object. For more information about Job Objects see https://aka.ms/job-objects")] 
    public static partial void RunningInsideJobObject(ILogger logger);

    [LoggerMessage(2, LogLevel.Information, "Resource Monitoring is running outside of Job Object. For more information about Job Objects see https://aka.ms/job-objects")] 
    public static partial void RunningOutsideJobObject(ILogger logger);
}
