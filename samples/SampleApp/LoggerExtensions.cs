using System;
using Microsoft.Framework.Logging;

namespace SampleApp
{
    internal static class LoggerExtensions
    {
        private static Func<ILogger, string, IDisposable> _purchaceOrderScope;
        private static Action<ILogger, DateTimeOffset, int, Exception> _programStarting;
        private static Action<ILogger, DateTimeOffset, Exception> _programStopping;

        static LoggerExtensions()
        {
            LoggerMessage.DefineScope(out _purchaceOrderScope, "PO:{PurchaceOrder}");
            LoggerMessage.Define(out _programStarting, LogLevel.Information, 1, "Starting", "at '{StartTime}' and 0x{Hello:X} is hex of 42");
            LoggerMessage.Define(out _programStopping, LogLevel.Information, 2, "Stopping", "at '{StopTime}'");
        }

        public static IDisposable PurchaceOrderScope(this ILogger logger, string purchaceOrder)
        {
            return _purchaceOrderScope(logger, purchaceOrder);
        }

        public static void ProgramStarting(this ILogger logger, DateTimeOffset startTime, int hello, Exception exception = null)
        {
            _programStarting(logger, startTime, hello, exception);
        }

        public static void ProgramStopping(this ILogger logger, DateTimeOffset stopTime, Exception exception = null)
        {
            _programStopping(logger, stopTime, exception);
        }
    }
}

