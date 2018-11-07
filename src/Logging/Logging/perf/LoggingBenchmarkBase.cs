// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.Performance
{
    public class LoggingBenchmarkBase
    {
        protected static readonly Action<ILogger, Exception> NoArgumentTraceMessage = LoggerMessage.Define(LogLevel.Trace, 0, "Message");
        protected static readonly Action<ILogger, Exception> NoArgumentErrorMessage = LoggerMessage.Define(LogLevel.Error, 0, "Message");

        protected static readonly Action<ILogger, int, string, Exception> TwoArgumentTraceMessage = LoggerMessage.Define<int, string>(LogLevel.Trace, 0, "Message {Argument1} {Argument2}");
        protected static readonly Action<ILogger, int, string, Exception> TwoArgumentErrorMessage = LoggerMessage.Define<int, string>(LogLevel.Error, 0, "Message {Argument1} {Argument2}");

        protected static Exception Exception = ((Func<Exception>)(() => {
            try
            {
                throw new Exception();
            }
            catch (Exception ex)
            {
                return ex;
            }
        }))();

        public class LoggerProvider<T>: ILoggerProvider
            where T: ILogger, new()
        {
            public void Dispose()
            {
            }

            public ILogger CreateLogger(string categoryName)
            {
                return new T();
            }
        }
    }
}