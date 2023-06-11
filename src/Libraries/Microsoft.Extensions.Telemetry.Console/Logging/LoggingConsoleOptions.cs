// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Telemetry.Console;

/// <summary>
/// Options to configure console logging formatter.
/// </summary>
[Experimental]
public class LoggingConsoleOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to display scopes.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true"/>.
    /// </value>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// Gets or sets the format string used to format timestamps in logging messages.
    /// </summary>
    /// <value>
    /// Defaults to <c>yyyy-MM-dd HH:mm:ss.fff</c>.
    /// </value>
    public string? TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>
    /// Gets or sets a value indicating whether or not UTC timezone should be used for timestamps in logging messages.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="false"/>.
    /// </value>
    public bool UseUtcTimestamp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to display the timestamp.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true"/>.
    /// </value>
    public bool IncludeTimestamp { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to display the log level.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true"/>.
    /// </value>
    public bool IncludeLogLevel { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to display the category.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true"/>.
    /// </value>
    public bool IncludeCategory { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to display the stack trace.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true"/>.
    /// </value>
    public bool IncludeExceptionStacktrace { get; set; } = true;

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
    /// Gets or sets a value indicating whether colors are enabled or not.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true"/>.
    /// </value>
    public bool ColorsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the color to use for dimmed text.
    /// </summary>
    /// <value>Defaults to <see cref="ConsoleColor.DarkGray"/>.</value>
    public ConsoleColor DimmedColor { get; set; } = ConsoleColor.DarkGray;

    /// <summary>
    /// Gets or sets the color to use for dimmed text background.
    /// </summary>
    public ConsoleColor? DimmedBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the color to use for exception text.
    /// </summary>
    /// <value>Defaults to <see cref="ConsoleColor.Red"/>.</value>
    public ConsoleColor ExceptionColor { get; set; } = ConsoleColor.Red;

    /// <summary>
    /// Gets or sets the color to use for exception text background.
    /// </summary>
    public ConsoleColor? ExceptionBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the color to use for exception stack trace text.
    /// </summary>
    /// <value>Defaults to <see cref="ConsoleColor.DarkRed"/>.</value>
    public ConsoleColor ExceptionStackTraceColor { get; set; } = ConsoleColor.DarkRed;

    /// <summary>
    /// Gets or sets the color to use for exception stack trace text background.
    /// </summary>
    public ConsoleColor? ExceptionStackTraceBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include dimension name/value pairs with each log record.
    /// </summary>
    /// <value>Defaults to <see langword="false"/>.</value>
    public bool IncludeDimensions { get; set; }

    /// <summary>
    /// Gets or sets the color to use for dimension text.
    /// </summary>
    /// <value>Defaults to <see cref="ConsoleColor.DarkGreen"/>.</value>
    public ConsoleColor DimensionsColor { get; set; } = ConsoleColor.DarkGreen;

    /// <summary>
    /// Gets or sets the color to use for dimension text background.
    /// </summary>
    public ConsoleColor? DimensionsBackgroundColor { get; set; }
}
#endif
