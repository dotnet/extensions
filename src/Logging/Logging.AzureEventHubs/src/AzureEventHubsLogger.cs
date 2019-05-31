// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.AzureEventHubs
{
    internal class AzureEventHubsLogger : ILogger
    {
        private readonly string _name;
        private readonly IAzureEventHubsLoggerFormatter _formatter;
        private readonly IAzureEventHubsLoggerProcessor _processor;

        internal AzureEventHubsLogger(string name, IAzureEventHubsLoggerFormatter formatter, IAzureEventHubsLoggerProcessor loggerProcessor)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _formatter = formatter;
            _processor = loggerProcessor;
        }

        internal IExternalScopeProvider ScopeProvider { get; set; }

        internal AzureEventHubsLoggerOptions Options { get; set; }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var eventData = _formatter.Format(logLevel, _name, eventId, state, exception, formatter, ScopeProvider);

            if (eventData != null)
            {
                _processor.Process(eventData);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public IDisposable BeginScope<TState>(TState state) => ScopeProvider?.Push(state) ?? NullScope.Instance;
    }
}
