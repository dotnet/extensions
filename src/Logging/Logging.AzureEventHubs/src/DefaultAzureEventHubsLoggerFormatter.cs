// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.Azure.EventHubs;

namespace Microsoft.Extensions.Logging.AzureEventHubs
{
    internal class DefaultAzureEventHubsLoggerFormatter : IAzureEventHubsLoggerFormatter
    {
        private static readonly string _loglevelPadding = ": ";
        private static readonly string _categoryPadding = " -> ";

        private StringBuilder _logBuilder;

        public EventData Format<TState>(LogLevel logLevel, string name, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter, IExternalScopeProvider scopeProvider)
        {
            var logBuilder = _logBuilder;
            _logBuilder = null;

            if (logBuilder == null)
            {
                logBuilder = new StringBuilder();
            }

            // Example:
            // INFO: ConsoleApp.Program[10] -> Request received

            logBuilder.Append(GetLogLevelString(logLevel));

            // category and event id
            logBuilder.Append(_loglevelPadding);
            logBuilder.Append(name);
            logBuilder.Append("[");
            logBuilder.Append(eventId);
            logBuilder.Append("]");

            // message
            logBuilder.Append(_categoryPadding);
            var message = formatter(state, exception);
            logBuilder.Append(message);

            var eventData = new EventData(Encoding.UTF8.GetBytes(logBuilder.ToString()));

            logBuilder.Clear();
            if (logBuilder.Capacity > 1024)
            {
                logBuilder.Capacity = 1024;
            }
            _logBuilder = logBuilder;

            return eventData;
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return "trce";

                case LogLevel.Debug:
                    return "dbug";

                case LogLevel.Information:
                    return "info";

                case LogLevel.Warning:
                    return "warn";

                case LogLevel.Error:
                    return "fail";

                case LogLevel.Critical:
                    return "crit";

                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }
    }
}
