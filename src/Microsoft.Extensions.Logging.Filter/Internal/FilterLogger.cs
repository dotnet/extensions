// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Logging.Filter.Internal
{
    public class FilterLogger : ILogger
    {
        private readonly ILogger _innerLogger;
        private readonly string _categoryName;
        private IFilterLoggerSettings _settings;
        private Func<LogLevel, bool> _filter;

        public FilterLogger(ILogger innerLogger, string categoryName, IFilterLoggerSettings settings)
        {
            _innerLogger = innerLogger;
            _categoryName = categoryName;
            _settings = settings;

            _filter = GetFilter();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _filter(logLevel);
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                _innerLogger.Log(logLevel, eventId, state, exception, formatter);
            }
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return _innerLogger.BeginScope(state);
        }

        private Func<LogLevel, bool> GetFilter()
        {
            foreach (var prefix in GetKeyPrefixes(_categoryName))
            {
                LogLevel level;
                if (_settings.TryGetSwitch(prefix, out level))
                {
                    return logLevel => logLevel >= level;
                }
            }

            return _ => true;
        }

        // Get the category name from most specific to least specific
        // Example: For "Microsoft.AspNetCore.Routing", the keys in order are
        // Microsoft.AspNetCore.Routing
        // Microsoft.AspNetCore
        // Microsoft
        private IEnumerable<string> GetKeyPrefixes(string name)
        {
            while (!string.IsNullOrEmpty(name))
            {
                yield return name;
                var lastIndexOfDot = name.LastIndexOf('.');
                if (lastIndexOfDot == -1)
                {
                    yield return "Default";
                    break;
                }
                name = name.Substring(0, lastIndexOfDot);
            }
        }
    }
}

