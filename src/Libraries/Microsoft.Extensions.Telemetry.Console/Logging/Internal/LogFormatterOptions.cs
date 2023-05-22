// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER

using Microsoft.Extensions.Logging.Console;

namespace Microsoft.Extensions.Telemetry.Console.Internal;

/// <summary>
/// Options to configure logging formatter.
/// </summary>
internal sealed class LogFormatterOptions : ConsoleFormatterOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogFormatterOptions"/> class.
    /// </summary>
    public LogFormatterOptions()
    {
        IncludeScopes = true;
        TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
        UseUtcTimestamp = false;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to display timestamp.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="true"/>.
    /// </remarks>
    public bool IncludeTimestamp { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to display log level.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="true"/>.
    /// </remarks>
    public bool IncludeLogLevel { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to display activity TraceId.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="true"/>.
    /// </remarks>
    public bool IncludeTraceId { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to display activity SpanId.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="true"/>.
    /// </remarks>
    public bool IncludeSpanId { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to display category.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="true"/>.
    /// </remarks>
    public bool IncludeCategory { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to display stack trace.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="true"/>.
    /// </remarks>
    public bool IncludeExceptionStacktrace { get; set; } = true;
}
#endif
