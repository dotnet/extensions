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
    }
}
