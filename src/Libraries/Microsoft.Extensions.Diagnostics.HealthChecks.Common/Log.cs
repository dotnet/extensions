﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

internal static partial class Log
{
    [LoggerMessage(0, LogLevel.Warning, "Process reporting unhealthy: {Status}. Health check entries are {Entries}")] 
    public static partial void Unhealthy(
        ILogger logger,
        HealthStatus status,
        StringBuilder entries);

    [LoggerMessage(1, LogLevel.Debug, "Process reporting healthy: {Status}.")] 
    public static partial void Healthy(
        ILogger logger,
        HealthStatus status);
}
