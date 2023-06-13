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
    /// Gets or sets a value indicating whether to display the timestamp.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool IncludeTimestamp { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to display the log level.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool IncludeLogLevel { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to display the activity TraceId.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool IncludeTraceId { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to display the activity SpanId.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool IncludeSpanId { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to display the category.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool IncludeCategory { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to display the stack trace.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool IncludeExceptionStacktrace { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to display the dimensions.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool IncludeDimensions { get; set; }
}
#endif
