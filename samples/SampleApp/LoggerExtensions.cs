using System;
using Microsoft.Framework.Logging;

namespace SampleApp
{
    internal static class LoggerExtensions
    {
        private static Action<ILogger, DateTimeOffset, int, Exception> _starting;
        private static Action<ILogger, DateTimeOffset, Exception> _stopping;

        static LoggerExtensions()
        {
            LoggerMessage.Define(out _starting, LogLevel.Information, 1, "Starting", "at '{StartTime}' and 0x{Hello:X} is hex of 42");
            LoggerMessage.Define(out _stopping, LogLevel.Information, 2, "Stopping", "at '{StopTime}'");
        }

        public static void Starting(this ILogger logger, DateTimeOffset startTime, int hello, Exception exception = null)
        {
            _starting(logger, startTime, hello, exception);
        }

        public static void Stopping(this ILogger logger, DateTimeOffset stopTime, Exception exception = null)
        {
            _stopping(logger, stopTime, exception);
        }
    }
}

