using System;
using NLog;

namespace Microsoft.Framework.Logging.NLog
{
    public class NLogLoggerProvider : ILoggerProvider
    {
        private readonly LogFactory _logFactory;

        public NLogLoggerProvider(global::NLog.LogFactory logFactory)
        {
            _logFactory = logFactory;
        }

        public ILogger Create(string name)
        {
            return new Logger(_logFactory.GetLogger(name));
        }

        class Logger : ILogger
        {
            private readonly global::NLog.Logger _logger;

            public Logger(global::NLog.Logger logger)
            {
                _logger = logger;
            }

            public bool WriteCore(
                TraceType eventType,
                int eventId,
                object state,
                Exception exception,
                Func<object, Exception, string> formatter)
            {
                var logLevel = GetLogLevel(eventType);
                if (!_logger.IsEnabled(logLevel))
                {
                    return false;
                }
                if (formatter == null)
                {
                    return true;
                }
                var message = formatter(state, exception);
                var eventInfo = LogEventInfo.Create(logLevel, _logger.Name, message, exception);
                eventInfo.Properties["EventId"] = eventId;
                _logger.Log(eventInfo);
                return true;
            }

            private LogLevel GetLogLevel(TraceType eventType)
            {
                switch (eventType)
                {
                    case TraceType.Verbose: return LogLevel.Debug;
                    case TraceType.Information: return LogLevel.Info;
                    case TraceType.Warning: return LogLevel.Warn;
                    case TraceType.Error: return LogLevel.Error;
                    case TraceType.Critical: return LogLevel.Fatal;
                }
                return LogLevel.Debug;
            }

            public IDisposable BeginScope(object state)
            {
                return NestedDiagnosticsContext.Push(state.ToString());
            }
        }
    }
}
