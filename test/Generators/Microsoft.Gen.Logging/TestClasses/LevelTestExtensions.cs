// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    internal static partial class LevelTestExtensions
    {
        [LogMethod(0, LogLevel.Trace, "M0")]
        public static partial void M0(ILogger logger);

        [LogMethod(1, LogLevel.Debug, "M1")]
        public static partial void M1(ILogger logger);

        [LogMethod(2, LogLevel.Information, "M2")]
        public static partial void M2(ILogger logger);

        [LogMethod(3, LogLevel.Warning, "M3")]
        public static partial void M3(ILogger logger);

        [LogMethod(4, LogLevel.Error, "M4")]
        public static partial void M4(ILogger logger);

        [LogMethod(5, LogLevel.Critical, "M5")]
        public static partial void M5(ILogger logger);

        [LogMethod(6, LogLevel.None, "M6")]
        public static partial void M6(ILogger logger);

        [LogMethod(7, (LogLevel)42, "M7")]
        public static partial void M7(ILogger logger);

        [LogMethod(8, "M8")]
        public static partial void M8(ILogger logger, LogLevel level);

        [LogMethod(9, "M9")]
        public static partial void M9(LogLevel level, ILogger logger);

#pragma warning disable R9G001 // Don't include a template for level in the logging message
        [LogMethod(10, "M10 {level}")]
        public static partial void M10(ILogger logger, LogLevel level);
#pragma warning restore R9G001

#pragma warning disable R9G017 // Don't include a template for logger in the logging message
        [LogMethod(11, "M11 {logger}")]
        public static partial void M11(ILogger logger, LogLevel level);
#pragma warning restore R9G017
    }
}
