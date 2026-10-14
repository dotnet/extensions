// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Limits how many log records a logging method emits within a time window.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to a logging method that has the <see cref="LoggerMessageAttribute"/>. The limit behaves like a
/// sliding window rate limiter without a queue: the window is split into <see cref="SegmentsPerWindow"/> equal segments,
/// and a call is logged only if the segments covering the last window hold fewer than <see cref="PermitLimit"/> logged calls.
/// Other calls are dropped. With one segment, the count resets once per window.
/// </para>
/// <para>
/// The limit is shared by all callers of the logging method in the process. It's checked after
/// <see cref="ILogger.IsEnabled(LogLevel)"/> and before any other work is done to create the log record.
/// </para>
/// <para>
/// If the logging method has a parameter of type <see cref="System.Diagnostics.Metrics.Meter"/>, each call dropped by the limit
/// is counted on that meter by a counter named <c>logging.rate_limiting.dropped</c>.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// [LoggerMessage(0, LogLevel.Error, "Error querying the database {hostName}")]
/// [RateLimit(5, SegmentsPerWindow = 4)] // At most 5 per second; the window moves every 250 milliseconds.
/// public static partial void DatabaseError(ILogger logger, Exception exception, string hostName);
/// </code>
/// </example>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
[AttributeUsage(AttributeTargets.Method)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class RateLimitAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitAttribute"/> class.
    /// </summary>
    /// <param name="permitLimit">The maximum number of log records the logging method emits per window.</param>
    public RateLimitAttribute(int permitLimit)
    {
        PermitLimit = permitLimit;
    }

    /// <summary>
    /// Gets the maximum number of log records the logging method emits per window.
    /// </summary>
    public int PermitLimit { get; }

    /// <summary>
    /// Gets or sets the length of the window, in milliseconds.
    /// </summary>
    /// <value>
    /// The default value is 1000.
    /// </value>
    public int WindowMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the number of equal segments the window is split into.
    /// </summary>
    /// <value>
    /// The default value is 1, which resets the count once per window.
    /// </value>
    public int SegmentsPerWindow { get; set; } = 1;
}
