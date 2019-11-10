// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    internal static class LoggerExtensions
    {
        private static Func<ILogger, string, IDisposable> _purchaseOrderScope = LoggerMessage.DefineScope<string>("PO:{PurchaseOrder}");
        private static LogMessage<DateTimeOffset, int> _programStarting = (LogLevel.Information, nameof(ProgramStarting), "Starting at '{StartTime}' and 0x{Hello:X} is hex of 42");
        private static LogMessage<DateTimeOffset> _programStopping = (LogLevel.Information, nameof(ProgramStopping), "Stopping at '{StopTime}'");

        public static IDisposable PurchaseOrderScope(this ILogger logger, string purchaseOrder)
        {
            return _purchaseOrderScope(logger, purchaseOrder);
        }

        public static void ProgramStarting(this ILogger logger, DateTimeOffset startTime, int hello)
        {
            _programStarting.Log(logger, startTime, hello);
        }

        public static void ProgramStopping(this ILogger logger, DateTimeOffset stopTime)
        {
            _programStopping.Log(logger, stopTime);
        }
    }
}
