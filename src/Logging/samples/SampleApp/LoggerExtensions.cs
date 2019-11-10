// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    internal static class LoggerExtensions
    {
        private static LogScope<string> _programPurchaceOrderScope = "PO:{PurchaseOrder}";
        private static LogMessage<DateTimeOffset, int> _programExampleStarting = (LogLevel.Information, nameof(ExampleStarting), "Starting at '{StartTime}' and 0x{Hello:X} is hex of 42");
        private static LogMessage<DateTimeOffset> _programExampleStopping = (LogLevel.Information, nameof(ExampleStopping), "Stopping at '{StopTime}'");

        public static IDisposable PurchaseOrderScope(this ILogger<Program> logger, string purchaseOrder) => _programPurchaceOrderScope.Begin(logger, purchaseOrder);

        public static void ExampleStarting(this ILogger<Program> logger, DateTimeOffset startTime, int hello) => _programExampleStarting.Log(logger, startTime, hello);

        public static void ExampleStopping(this ILogger<Program> logger, DateTimeOffset stopTime) => _programExampleStopping.Log(logger, stopTime);
    }
}
