// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using NLog;

namespace Microsoft.Framework.Logging.NLog
{
    public class NLogLoggerProvider : ILoggerProvider
    {
        private readonly LogFactory _logFactory;

        public NLogLoggerProvider(LogFactory logFactory)
        {
            _logFactory = logFactory;
        }

        public ILogger Create(string name)
        {
            return new Logger(_logFactory.GetLogger(name));
        }

        private class Logger : ILogger
        {
            private readonly global::NLog.Logger _logger;

            public Logger(global::NLog.Logger logger)
            {
                _logger = logger;
            }

            public void Write(
                LogLevel logLevel,
                int eventId,
                object state,
                Exception exception,
                Func<object, Exception, string> formatter)
            {
                var nLogLogLevel = GetLogLevel(logLevel);
                var message = string.Empty;
                if (formatter != null)
                {
                    message = formatter(state, exception);
                }
                else
                {
                    if (state != null)
                    {
                        message += state;
                    }
                    if (exception != null)
                    {
                        message += Environment.NewLine + exception;
                    }
                }
                if (!string.IsNullOrEmpty(message))
                {
                    var eventInfo = LogEventInfo.Create(nLogLogLevel, _logger.Name, message, exception);
                    eventInfo.Properties["EventId"] = eventId;
                    _logger.Log(eventInfo);
                }
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return _logger.IsEnabled(GetLogLevel(logLevel));
            }

            private global::NLog.LogLevel GetLogLevel(LogLevel logLevel)
            {
                switch (logLevel)
                {
                    case LogLevel.Verbose: return global::NLog.LogLevel.Debug;
                    case LogLevel.Information: return global::NLog.LogLevel.Info;
                    case LogLevel.Warning: return global::NLog.LogLevel.Warn;
                    case LogLevel.Error: return global::NLog.LogLevel.Error;
                    case LogLevel.Critical: return global::NLog.LogLevel.Fatal;
                }
                return global::NLog.LogLevel.Debug;
            }

            public IDisposable BeginScope(object state)
            {
                return NestedDiagnosticsContext.Push(state.ToString());
            }
        }
    }
}
