// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.AspNetCore.Telemetry.Internal;

#pragma warning disable S109 // Magic numbers should not be used
internal static partial class Log
{
    /// <summary>
    /// Logs `Http Route not found for Activity {activityName}.` at `Debug` level.
    /// </summary>
    [LogMethod(2, LogLevel.Debug, "Http Route not found for Activity {activityName}.")]
    public static partial void HttpRouteNotFound(this ILogger logger, string activityName);

    /// <summary>
    /// Logs `Configured HttpTracingOptions: {options}` at `Information` level.
    /// </summary>
    [LogMethod(3, LogLevel.Information, "Configured HttpTracingOptions: {options}")]
    internal static partial void ConfiguredHttpTracingOptions(this ILogger logger, HttpTracingOptions options);
}
#pragma warning restore S109 // Magic numbers should not be used
