// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Framework.Logging.Console.Internal;

namespace Microsoft.Framework.Logging.Console
{
    public class ConsoleLogger : ILogger
    {
        private const int _indentation = 2;
        private readonly string _name;
        private Func<string, LogLevel, bool> _filter;
        private readonly object _lock = new object();

        private static readonly Dictionary<LogLevel, string> _logLevelMappings = new Dictionary<LogLevel, string>()
        {
            { LogLevel.Information, "info" },
            { LogLevel.Critical, "critical" },
            { LogLevel.Debug, "debug" },
            { LogLevel.Error, "error" },
            { LogLevel.Verbose, "verbose" },
            { LogLevel.Warning, "warning" }
        };
        private static readonly string UnknownLogLevel = "unknown";
        private static readonly int Padding;

        static ConsoleLogger()
        {
            Padding = Math.Max(
                _logLevelMappings.Values.Max(v => v.Length),
                UnknownLogLevel.Length);
        }

        public ConsoleLogger(string name, Func<string, LogLevel, bool> filter)
        {
            _name = name;
            _filter = filter ?? ((category, logLevel) => true);
            Console = new LogConsole();
        }

        public IConsole Console { get; set; }
        protected string Name { get { return _name; } }

        public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            var message = string.Empty;
            var values = state as ILogValues;
            if (formatter != null)
            {
                message = formatter(state, exception);
            }
            else if (values != null)
            {
                var builder = new StringBuilder();
                FormatLogValues(
                    builder,
                    values,
                    level: 1,
                    bullet: false);
                message = builder.ToString();
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
            lock (_lock)
            {
                var originalForegroundColor = Console.ForegroundColor;  // save current colors
                var originalBackgroundColor = Console.BackgroundColor;
                SetConsoleColor(logLevel);
                try
                {
                    Console.WriteLine(FormatMessage(logLevel, message));
                }
                finally
                {
                    Console.ForegroundColor = originalForegroundColor;  // reset initial colors
                    Console.BackgroundColor = originalBackgroundColor;
                }
            }
        }

        private string FormatMessage(LogLevel logLevel, string message)
        {
            string logLevelString;
            if(!_logLevelMappings.TryGetValue(logLevel, out logLevelString))
            {
                logLevelString = UnknownLogLevel;
            }
            return $"{logLevelString.PadRight(Padding)}: [{_name}] {message}";
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _filter(_name, logLevel);
        }

        // sets the console text color to reflect the given LogLevel
        private void SetConsoleColor(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Information:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogLevel.Verbose:
                default:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }
        }

        public IDisposable BeginScopeImpl(object state)
        {
            return null;
        }

        private void FormatLogValues(StringBuilder builder, ILogValues logValues, int level, bool bullet)
        {
            var values = logValues.GetValues();
            if (values == null)
            {
                return;
            }
            var isFirst = true;
            foreach (var kvp in values)
            {
                builder.AppendLine();
                if (bullet && isFirst)
                {
                    builder.Append(' ', level * _indentation - 1)
                           .Append('-');
                }
                else
                {
                    builder.Append(' ', level * _indentation);
                }
                builder.Append(kvp.Key)
                       .Append(": ");
                if (kvp.Value is IEnumerable && !(kvp.Value is string))
                {
                    foreach (var value in (IEnumerable)kvp.Value)
                    {
                        if (value is ILogValues)
                        {
                            FormatLogValues(
                                builder,
                                (ILogValues)value,
                                level + 1,
                                bullet: true);
                        }
                        else
                        {
                            builder.AppendLine()
                                   .Append(' ', (level + 1) * _indentation)
                                   .Append(value);
                        }
                    }
                }
                else if (kvp.Value is ILogValues)
                {
                    FormatLogValues(
                        builder,
                        (ILogValues)kvp.Value,
                        level + 1,
                        bullet: false);
                }
                else
                {
                    builder.Append(kvp.Value);
                }
                isFirst = false;
            }
        }
    }
}