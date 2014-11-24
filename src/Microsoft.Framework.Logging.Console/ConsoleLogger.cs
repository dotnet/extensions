// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Globalization;
using System.Text;

namespace Microsoft.Framework.Logging.Console
{
    public class ConsoleLogger : ILogger
    {
        private const int _indentation = 2;
        private readonly string _name;
        private Func<string, LogLevel, bool> _filter;
        private readonly object _lock = new object();

        public ConsoleLogger(string name, Func<string, LogLevel, bool> filter)
        {
            _name = name;
            _filter = filter ?? ((category, logLevel) => true);
            Console = new LogConsole();
        }

        public IConsole Console { get; set; }

        public void Write(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            var message = string.Empty;
            var structure = state as ILoggerStructure;
            if (structure != null)
            {
                var builder = new StringBuilder();
                FormatLoggerStructure(
                    builder,
                    structure,
                    level: 1,
                    bullet: false);
                message = Convert.ToString(builder.ToString(), CultureInfo.InvariantCulture);
            }
            else if (formatter != null)
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
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            lock (_lock)
            {
                var originalForegroundColor = Console.ForegroundColor;  // save current colors
                var originalBackgroundColor = Console.BackgroundColor;
                var severity = logLevel.ToString().ToUpperInvariant();
                SetConsoleColor(logLevel);
                try
                {
                    Console.WriteLine("[{0}:{1}] {2}", severity, _name, message);
                }
                finally
                {
                    Console.ForegroundColor = originalForegroundColor;  // reset initial colors
                    Console.BackgroundColor = originalBackgroundColor;
                }
            }
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

        public IDisposable BeginScope(object state)
        {
            return null;
        }

        private void FormatLoggerStructure(StringBuilder builder, ILoggerStructure structure, int level, bool bullet)
        {
            if (structure.Message != null)
            {
                builder.Append(structure.Message);
            }
            var values = structure.GetValues();
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
                        if (value is ILoggerStructure)
                        {
                            FormatLoggerStructure(
                                builder,
                                (ILoggerStructure)value,
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
                else if (kvp.Value is ILoggerStructure)
                {
                    FormatLoggerStructure(
                        builder,
                        (ILoggerStructure)kvp.Value,
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