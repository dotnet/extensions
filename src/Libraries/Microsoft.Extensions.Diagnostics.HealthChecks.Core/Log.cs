// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

internal static partial class Log
{
    [LogMethod(0, LogLevel.Warning, "Process reporting unhealthy: {status}. Health check entries are {entries}")]
    public static partial void Unhealthy(
        ILogger logger,
        HealthStatus status,
        StringBuilder entries);

    [LogMethod(1, LogLevel.Debug, "Process reporting healthy: {status}.")]
    public static partial void Healthy(
        ILogger logger,
        HealthStatus status);
}
