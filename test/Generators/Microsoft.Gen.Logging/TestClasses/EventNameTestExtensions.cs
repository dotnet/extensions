// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace TestClasses
{
    internal static partial class EventNameTestExtensions
    {
        [LoggerMessage(0, LogLevel.Trace, "M0", EventName = "CustomEventName")]
        public static partial void M0(ILogger logger);

        [LoggerMessage(EventName = "M1_Event")]
        public static partial void M1(LogLevel level, ILogger logger, string p0);

        // This one should have the same generated EventId as the method above
        [LoggerMessage(LogLevel.Debug)]
        public static partial void M1_Event(ILogger logger, string p0);

        // This one should have different generated EventId as the methods above
        [LoggerMessage(LogLevel.Error)]
        public static partial void M2(ILogger logger, string p0);
    }
}
