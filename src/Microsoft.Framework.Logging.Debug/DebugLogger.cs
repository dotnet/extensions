// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Framework.Logging.Debug
{
    public class DebugLogger : ILogger
    {
        private readonly Func<string, LogLevel, bool> _filter;
        private readonly string _name;

        public DebugLogger(string name)
            : this(name, filter: null)
        {
        }

        public DebugLogger(string name, Func<string, LogLevel, bool> filter)
        {
            _name = string.IsNullOrEmpty(name) ? nameof(DebugLogger) : name;
            _filter = filter ?? ((category, logLevel) => true);
        }

        public IDisposable BeginScopeImpl(object state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (!Debugger.IsAttached)
            {
                return false;
            }

            return _filter(_name, logLevel);
        }

        public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
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

            message = $"{ logLevel }: {message}";
            System.Diagnostics.Debug.WriteLine(message, _name);
        }
    }
}
