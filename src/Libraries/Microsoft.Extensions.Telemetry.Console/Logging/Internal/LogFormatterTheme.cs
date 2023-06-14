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
    /// <value>Default is <see langword="true"/>.</value>
    public bool ColorsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the color to use for dimmed text.
    /// </summary>
    /// <value>The default value is <see cref="Colors.DarkGrayOnNone"/>.</value>
    public ColorSet Dimmed { get; set; } = Colors.DarkGrayOnNone;

    /// <summary>
    /// Gets or sets the color to use for exception text.
    /// </summary>
    /// <value>Default is <see cref="Colors.RedOnNone"/>.</value>
    public ColorSet Exception { get; set; } = Colors.RedOnNone;

    /// <summary>
    /// Gets or sets the  color to use for exception stack trace.
    /// </summary>
    /// <value>Default is <see cref="Colors.DarkRedOnNone"/>.</value>
    public ColorSet ExceptionStackTrace { get; set; } = Colors.DarkRedOnNone;

    /// <summary>
    /// Gets or sets the color to use for dimensions.
    /// </summary>
    /// <value>Default is <see cref="Colors.DarkGreenOnNone"/>.</value>
    public ColorSet Dimensions { get; set; } = Colors.DarkGreenOnNone;
}

#endif
