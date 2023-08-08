// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    internal static partial class MessageTestExtensions
    {
        [LogMethod(0, LogLevel.Trace, null!)]
        public static partial void M0(ILogger logger);

        [LogMethod(1, LogLevel.Debug, "")]
        public static partial void M1(ILogger logger);

        [LogMethod(2, LogLevel.Debug)]
        public static partial void M2(ILogger logger);

#if false

        // These are disabled due to https://github.com/dotnet/roslyn/issues/52527
        //
        // These are handled fine by the logger generator and generate warnings as expected. Unfortunately, the above warning suppression is
        // not being observed by the C# compiler at the moment, so having these here causes build warnings.

        [LogMethod(2, LogLevel.Trace)]
        public static partial void M2(ILogger logger, string p1, string p2);

        [LogMethod(3, LogLevel.Debug, "")]
        public static partial void M3(ILogger logger, string p1, int p2);

        [LogMethod(4, LogLevel.Debug, "{p1}")]
        public static partial void M4(ILogger logger, string p1, int p2, int p3);

#endif

        [LogMethod(5, LogLevel.Debug, "\"Hello\" World")]
        public static partial void M5(ILogger logger);

        [LogMethod(6, "\"{Value1}\" -> \"{Value2}\"")]
        public static partial void M6(ILogger logger, LogLevel logLevel, string value1, string value2);

        [LogMethod(7, LogLevel.Debug, "\"\n\r\\")]
        public static partial void M7(ILogger logger);
    }
}
