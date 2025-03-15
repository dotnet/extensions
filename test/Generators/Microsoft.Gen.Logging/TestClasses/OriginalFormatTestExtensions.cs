// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace TestClasses
{
    internal static partial class OriginalFormatTestExtensions
    {
        [LoggerMessage(0, LogLevel.Information, "M0")]
        public static partial void M0(ILogger logger);

        [LoggerMessage(1, LogLevel.Information, "M1 {p0}")]
        public static partial void M1(ILogger logger, string p0);

        [LoggerMessage(2, LogLevel.Information, "M2 {p0}{p1}")]
        public static partial void M2(ILogger logger, string p0, string p1);

        [LoggerMessage(LogLevel.Information)]
        public static partial void M3(ILogger logger, string p0, string p1, string p2);
    }
}
