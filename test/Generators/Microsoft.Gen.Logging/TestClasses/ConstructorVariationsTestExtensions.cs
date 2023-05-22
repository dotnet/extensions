// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    internal static partial class ConstructorVariationsTestExtensions
    {
        [LogMethod(0, LogLevel.Debug, "M0 {p0}")]
        public static partial void M0(ILogger logger, string p0);

        [LogMethod(1, "M1 {p0}")]
        public static partial void M1(ILogger logger, LogLevel level, string p0);

        [LogMethod(2, LogLevel.Debug)]
        public static partial void M2(ILogger logger, string p0);

        [LogMethod(3)]
        public static partial void M3(ILogger logger, LogLevel level, string p0);

        [LogMethod(LogLevel.Debug, "M4 {p0}")]
        public static partial void M4(ILogger logger, string p0);

        [LogMethod("M5 {p0}")]
        public static partial void M5(ILogger logger, LogLevel level, string p0);

        [LogMethod(LogLevel.Debug)]
        public static partial void M6(ILogger logger, string p0);

        [LogMethod]
        public static partial void M7(ILogger logger, LogLevel level, string p0);
    }
}
