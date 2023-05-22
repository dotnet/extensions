// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Internal;

[SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used", Justification = "Event IDs.")]
internal static partial class Log
{
    /// <summary>
    /// Logs `Outgoing Http Request URI for Activity (Name = '{activityName}', Id = '{activityId}') was not set.` at `Error` level.
    /// </summary>
    [LogMethod(2, LogLevel.Error, "Outgoing Http Request URI for Activity (Name = '{activityName}', Id = '{activityId}') was not set.")]
    public static partial void HttpRequestUriWasNotSet(this ILogger logger, string activityName, string? activityId);

    /// <summary>
    /// Logs `Request metadata is not set for the request {absoluteUri}` at `Trace` level.
    /// </summary>
    [LogMethod(3, LogLevel.Trace, "Request metadata is not set for the request {absoluteUri}")]
    internal static partial void RequestMetadataIsNotSetForTheRequest(this ILogger logger, string absoluteUri);

    /// <summary>
    /// Logs `Configured HttpClientTracingOptions: {options}` at `Information` level.
    /// </summary>
    [LogMethod(4, LogLevel.Information, "Configured HttpClientTracingOptions: {options}")]
    internal static partial void ConfiguredHttpClientTracingOptions(this ILogger logger, HttpClientTracingOptions options);
}
