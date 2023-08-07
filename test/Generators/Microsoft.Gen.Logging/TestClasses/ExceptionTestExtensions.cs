// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    internal static partial class ExceptionTestExtensions
    {
        [LogMethod(0, LogLevel.Trace, "M0 {ex2}")]
        public static partial void M0(ILogger logger, Exception ex1, Exception ex2);

        [LogMethod(1, LogLevel.Debug, "M1 {ex2}")]
        public static partial void M1(Exception ex1, ILogger logger, Exception ex2);

#pragma warning disable LOGGEN009 // Don't include a template for ex in the logging message
        [LogMethod(2, LogLevel.Debug, "M2 {arg1}: {ex}")]
        public static partial void M2(ILogger logger, string arg1, Exception ex);
#pragma warning restore LOGGEN009

        [LogMethod]
        public static partial void M3(Exception ex, ILogger logger, LogLevel level);
    }
}
