// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Logging.EventLog
{
    /// <summary>
    /// A logger that writes messages to Windows Event Log.
    /// </summary>
    public class EventLogLogger : ILogger
    {
        private readonly System.Diagnostics.EventLog _eventLog;
        private readonly string _name;
        private readonly EventLogSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogLogger"/> class.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        public EventLogLogger(string name)
            : this(name, settings: new EventLogSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogLogger"/> class.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        /// <param name="settings">The <see cref="EventLogSettings"/>.</param>
        public EventLogLogger(string name, EventLogSettings settings)
        {
            _name = string.IsNullOrEmpty(name) ? nameof(EventLogLogger) : name;
            _settings = settings;

            var logName = string.IsNullOrEmpty(settings.LogName) ? "Application" : settings.LogName;
            var sourceName = string.IsNullOrEmpty(settings.SourceName) ? "Application" : settings.SourceName;
            var machineName = string.IsNullOrEmpty(settings.MachineName) ? "." : settings.MachineName;

            // Due to the following reasons, we cannot have these checks either here or in IsEnabled method:
            // 1. Log name & source name existence check only works on local computer.
            // 2. Source name existence check requires Administrative privileges.

            _eventLog = new System.Diagnostics.EventLog(logName, machineName, sourceName);
        }

        /// <inheritdoc />
        public IDisposable BeginScopeImpl(object state)
        {
            return new NoopDisposable();
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return _settings.Filter == null || _settings.Filter(_name, logLevel);
        }

        /// <inheritdoc />
        public void Log(
            LogLevel logLevel,
            int eventId,
            object state,
            Exception exception,
            Func<object, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string message;
            var values = state as ILogValues;
            if (formatter != null)
            {
                message = formatter(state, exception);
            }
            else if (values != null)
            {
                message = LogFormatter.FormatLogValues(values);
                if (exception != null)
                {
                    message += Environment.NewLine + exception;
                }
            }
            else
            {
                message = LogFormatter.Formatter(state, exception);
            }

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            message = _name + Environment.NewLine + message;

            // category '0' translates to 'None' in event log
            _eventLog.WriteEntry(message, GetEventLogEntryType(logLevel), eventId, category: 0);
        }

        private EventLogEntryType GetEventLogEntryType(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Information:
                case LogLevel.Debug:
                case LogLevel.Verbose:
                    return EventLogEntryType.Information;
                case LogLevel.Warning:
                    return EventLogEntryType.Warning;
                case LogLevel.Critical:
                case LogLevel.Error:
                    return EventLogEntryType.Error;
                default:
                    return EventLogEntryType.Information;
            }
        }

        private class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
