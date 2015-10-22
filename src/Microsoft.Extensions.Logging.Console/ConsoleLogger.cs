// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Text;
using Microsoft.Extensions.Logging.Console.Internal;

namespace Microsoft.Extensions.Logging.Console
{
    public class ConsoleLogger : ILogger
    {
        // Writing to console is not an atomic operation in the current implementation and since multiple logger
        // instances are created with a different name. Also since Console is global, using a static lock is fine.
        private static readonly object _lock = new object();
        private static readonly string _loglevelPadding = ": ";
        private static readonly string _messagePadding;

        private const int _indentation = 2;
        private readonly string _name;
        private readonly Func<string, LogLevel, bool> _filter;

        static ConsoleLogger()
        {
            var logLevelString = GetLogLevelString(LogLevel.Information);
            _messagePadding = new string(' ', logLevelString.Length + _loglevelPadding.Length);
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

            WriteMessage(logLevel, _name, eventId, message);
        }

        public virtual void WriteMessage(LogLevel logLevel, string logName, int eventId, string message)
        {
            // check if the message has any new line characters in it and provide the padding if necessary
            message = message.Replace(Environment.NewLine, Environment.NewLine + _messagePadding);
            var logLevelColors = GetLogLevelConsoleColors(logLevel);
            var loglevelString = GetLogLevelString(logLevel);

            // Example:
            // INFO: ConsoleApp.Program[10]
            //       Request received

            lock (_lock)
            {
                // log level string
                WriteWithColor(
                    logLevelColors.Foreground,
                    logLevelColors.Background,
                    loglevelString,
                    newLine: false);

                // category and event id
                // use default colors
                WriteWithColor(
                    ConsoleColor.Gray,
                    Console.BackgroundColor,
                    _loglevelPadding + logName + $"[{eventId}]",
                    newLine: true);

                // message
                WriteWithColor(
                    ConsoleColor.White,
                    Console.BackgroundColor,
                    _messagePadding + message,
                    newLine: true);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _filter(_name, logLevel);
        }

        public IDisposable BeginScopeImpl(object state)
        {
            return new NoopDisposable();
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

        private static string GetLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    return "dbug";
                case LogLevel.Verbose:
                    return "verb";
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

        private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
        {
            // do not change user's background color except for Critical
            switch (logLevel)
            {
                case LogLevel.Critical:
                    return new ConsoleColors(ConsoleColor.White, ConsoleColor.Red);
                case LogLevel.Error:
                    return new ConsoleColors(ConsoleColor.Red, Console.BackgroundColor);
                case LogLevel.Warning:
                    return new ConsoleColors(ConsoleColor.DarkYellow, Console.BackgroundColor);
                case LogLevel.Information:
                    return new ConsoleColors(ConsoleColor.DarkGreen, Console.BackgroundColor);
                case LogLevel.Debug:
                case LogLevel.Verbose:
                default:
                    return new ConsoleColors(ConsoleColor.Gray, Console.BackgroundColor);
            }
        }

        private void WriteWithColor(
            ConsoleColor foreground,
            ConsoleColor background,
            string message,
            bool newLine = false)
        {
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;

            try
            {
                if (newLine)
                {
                    Console.WriteLine(message);
                }
                else
                {
                    Console.Write(message);
                }
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private struct ConsoleColors
        {
            public ConsoleColors(ConsoleColor foreground, ConsoleColor background)
            {
                Foreground = foreground;
                Background = background;
            }
            public ConsoleColor Foreground { get; }

            public ConsoleColor Background { get; }
        }

        private class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}