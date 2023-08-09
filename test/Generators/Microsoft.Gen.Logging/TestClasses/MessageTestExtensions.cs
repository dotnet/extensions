// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace TestClasses
{
    internal static partial class MessageTestExtensions
    {
        [LoggerMessage(0, LogLevel.Trace, null!)]
        public static partial void M0(ILogger logger);

        [LoggerMessage(1, LogLevel.Debug, "")]
        public static partial void M1(ILogger logger);

#pragma warning disable LOGGEN030
        [LoggerMessage(LogLevel.Debug)]
        public static partial void M2(ILogger logger);
#pragma warning restore LOGGEN030

        [LoggerMessage(LogLevel.Trace)]
        public static partial void M2(ILogger logger, string p1, string p2);

        [LoggerMessage(LogLevel.Debug, "")]
        public static partial void M3(ILogger logger, string p1, int p2);

        [LoggerMessage(LogLevel.Debug, "{p1}")]
        public static partial void M4(ILogger logger, string p1, int p2, int p3);

        [LoggerMessage(LogLevel.Debug, "\"Hello\" World")]
        public static partial void M5(ILogger logger);

        [LoggerMessage("\"{Value1}\" -> \"{Value2}\"")]
        public static partial void M6(ILogger logger, LogLevel logLevel, string value1, string value2);

        [LoggerMessage(LogLevel.Debug, "\"\n\r\\")]
        public static partial void M7(ILogger logger);
    }
}
