// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET5_0_OR_GREATER
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Console.Internal;

/// <summary>
/// An internal class of dotnet:
/// <see href="https://github.com/dotnet/runtime/blob/master/src/libraries/Microsoft.Extensions.Logging.Console/src/SimpleConsoleFormatter.cs">SimpleConsoleFormatter</see>.
/// </summary>
internal static class LogLevelExtensions
{
    public static string InShortString(this LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "eror",
            LogLevel.Critical => "crit",
            _ => "none"
        };
    }

    public static ColorSet InColor(this LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => Colors.GrayOnBlack,
            LogLevel.Debug => Colors.GrayOnBlack,
            LogLevel.Information => Colors.DarkGreenOnBlack,
            LogLevel.Warning => Colors.YellowOnBlack,
            LogLevel.Error => Colors.BlackOnDarkRed,
            LogLevel.Critical => Colors.WhiteOnDarkRed,
            _ => Colors.None
        };
    }
}
#endif
