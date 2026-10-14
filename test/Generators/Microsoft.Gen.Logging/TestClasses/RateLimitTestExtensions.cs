// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace TestClasses
{
    internal static partial class RateLimitTestExtensions
    {
        // A window this long never moves during a test run, so each method allows exactly its permit limit.
        // Each method is used by a single test, because a rate limit is shared by the whole process.
        private const int LongWindow = int.MaxValue;

        [LoggerMessage(0, LogLevel.Information, "Limited {p0}")]
        [LogRateLimit(2, WindowMilliseconds = LongWindow)]
        internal static partial void Limited(ILogger logger, string p0);

        [LoggerMessage(1, LogLevel.Error, "LimitedError {p0}")]
        [LogRateLimit(2, WindowMilliseconds = LongWindow)]
        internal static partial void LimitedError(ILogger logger, string p0);

        [LoggerMessage(2, LogLevel.Information, "LimitedWithMeter {p0}")]
        [LogRateLimit(1, WindowMilliseconds = LongWindow)]
        internal static partial void LimitedWithMeter(ILogger logger, Meter? meter, string p0);

        [LoggerMessage(3, LogLevel.Information, "LimitedWithNullMeter {p0}")]
        [LogRateLimit(1, WindowMilliseconds = LongWindow)]
        internal static partial void LimitedWithNullMeter(ILogger logger, Meter? meter, string p0);

        [LoggerMessage(4, LogLevel.Warning, "LimitedWithEventName {p0}", EventName = "RateLimitedEventName")]
        [LogRateLimit(1, WindowMilliseconds = LongWindow, SegmentsPerWindow = 4)]
        internal static partial void LimitedWithEventName(ILogger logger, string p0, Meter meter);

        [LoggerMessage(5, LogLevel.Error, "LimitedSkipEnabledCheck", SkipEnabledCheck = true)]
        [LogRateLimit(1, WindowMilliseconds = LongWindow)]
        internal static partial void LimitedSkipEnabledCheck(ILogger logger);

        [LoggerMessage(6, LogLevel.Information, "Overload {p0}")]
        [LogRateLimit(1, WindowMilliseconds = LongWindow)]
        internal static partial void Overload(ILogger logger, string p0);

        [LoggerMessage(7, LogLevel.Information, "Overload {p0} {p1}")]
        [LogRateLimit(1, WindowMilliseconds = LongWindow)]
        internal static partial void Overload(ILogger logger, string p0, int p1);
    }

    internal partial class RateLimitInstanceTestClass
    {
        private readonly ILogger _logger;

        public RateLimitInstanceTestClass(ILogger logger)
        {
            _logger = logger;
        }

        [LoggerMessage(0, LogLevel.Warning, "Instance {p0}")]
        [LogRateLimit(1, WindowMilliseconds = int.MaxValue)]
        public partial void Instance(string p0);
    }
}
