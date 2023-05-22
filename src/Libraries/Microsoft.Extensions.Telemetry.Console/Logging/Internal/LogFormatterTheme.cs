// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER

namespace Microsoft.Extensions.Telemetry.Console.Internal;

/// <summary>
/// Theme for logging formatter.
/// </summary>
internal sealed class LogFormatterTheme
{
    /// <summary>
    /// Gets or sets a value indicating whether colors are enabled or not.
    /// </summary>
    /// <remarks>Default is <see langword="true"/>.</remarks>
    public bool ColorsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating what color to use for dimmed text.
    /// </summary>
    /// <remarks>Default is <see cref="Colors.DarkGrayOnNone"/>.</remarks>
    public ColorSet Dimmed { get; set; } = Colors.DarkGrayOnNone;

    /// <summary>
    /// Gets or sets a value indicating what color to use for exception text.
    /// </summary>
    /// <remarks>Default is <see cref="Colors.RedOnNone"/>.</remarks>
    public ColorSet Exception { get; set; } = Colors.RedOnNone;

    /// <summary>
    /// Gets or sets a value indicating what color to use for exception stack trace.
    /// </summary>
    /// <remarks>Default is <see cref="Colors.DarkRedOnNone"/>.</remarks>
    public ColorSet ExceptionStackTrace { get; set; } = Colors.DarkRedOnNone;
}

#endif
