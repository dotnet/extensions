// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace TestClasses
{
    internal static partial class SkipEnabledCheckTestExtensions
    {
        [LoggerMessage(0, LogLevel.Information, "M0", SkipEnabledCheck = true)]
        internal static partial void LoggerMethodWithTrueSkipEnabledCheck(ILogger logger);

        [LoggerMessage(1, LogLevel.Information, "M1", SkipEnabledCheck = false)]
        internal static partial void LoggerMethodWithFalseSkipEnabledCheck(ILogger logger);

        // Default ctor:
        [LoggerMessage(SkipEnabledCheck = false)]
        internal static partial void LoggerMethodWithFalseSkipEnabledCheck(ILogger logger, LogLevel level, string p1);
    }
}
