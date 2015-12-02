// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Runtime.InteropServices;
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

        // ConsoleColor does not have a value to specify the 'Default' color
        private readonly ConsoleColor? DefaultConsoleColor = null;

        private const int _indentation = 2;

        private IConsole _console;
        private Func<string, LogLevel, bool> _filter;

        static ConsoleLogger()
        {
            var logLevelString = GetLogLevelString(LogLevel.Information);
            _messagePadding = new string(' ', logLevelString.Length + _loglevelPadding.Length);
        }

        public ConsoleLogger(string name, Func<string, LogLevel, bool> filter, bool includeScopes)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            
            Name = name;
            Filter = filter ?? ((category, logLevel) => true);
            IncludeScopes = includeScopes;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console = new WindowsLogConsole();
            }
            else
            {
                Console = new AnsiLogConsole(new AnsiSystemConsole());
            }
        }

        public IConsole Console
        {
            get { return _console; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _console = value;
            }
        }

        public Func<string, LogLevel, bool> Filter
        {
            get { return _filter; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _filter = value;
            }
        }

        public bool IncludeScopes { get; set; }

        public string Name { get; }

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

            WriteMessage(logLevel, Name, eventId, message);
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
                    DefaultConsoleColor,
                    _loglevelPadding + logName + $"[{eventId}]",
                    newLine: true);

                // scope information
                if (IncludeScopes)
                {
                    var scopeInformation = GetScopeInformation();
                    if (!string.IsNullOrEmpty(scopeInformation))
                    {
                        WriteWithColor(
                            ConsoleColor.Gray,
                            DefaultConsoleColor,
                            _messagePadding + scopeInformation,
                            newLine: true);
                    }
                }

                // message
                WriteWithColor(
                    ConsoleColor.White,
                    DefaultConsoleColor,
                    _messagePadding + message,
                    newLine: true);

                // In case of AnsiLogConsole, the messages are not yet written to the console,
                // this would flush them instead.
                Console.Flush();
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return Filter(Name, logLevel);
        }

        public IDisposable BeginScopeImpl(object state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return ConsoleLogScope.Push(Name, state);
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
                    return new ConsoleColors(ConsoleColor.Red, DefaultConsoleColor);
                case LogLevel.Warning:
                    return new ConsoleColors(ConsoleColor.DarkYellow, DefaultConsoleColor);
                case LogLevel.Information:
                    return new ConsoleColors(ConsoleColor.DarkGreen, DefaultConsoleColor);
                case LogLevel.Debug:
                case LogLevel.Verbose:
                default:
                    return new ConsoleColors(ConsoleColor.Gray, DefaultConsoleColor);
            }
        }

        private void WriteWithColor(
            ConsoleColor? foreground,
            ConsoleColor? background,
            string message,
            bool newLine = false)
        {
            if (newLine)
            {
                Console.WriteLine(message, background, foreground);
            }
            else
            {
                Console.Write(message, background, foreground);
            }
        }

        private string GetScopeInformation()
        {
            var current = ConsoleLogScope.Current;
            var output = new StringBuilder();
            string scopeLog = string.Empty;
            while (current != null)
            {
                if (output.Length == 0)
                {
                    scopeLog = $"=> {current}";
                }
                else
                {
                    scopeLog = $"=> {current} ";
                }

                output.Insert(0, scopeLog);
                current = current.Parent;
            }

            return output.ToString();
        }

        private struct ConsoleColors
        {
            public ConsoleColors(ConsoleColor? foreground, ConsoleColor? background)
            {
                Foreground = foreground;
                Background = background;
            }

            public ConsoleColor? Foreground { get; }

            public ConsoleColor? Background { get; }
        }

        private class AnsiSystemConsole : IAnsiSystemConsole
        {
            public void Write(string message)
            {
                System.Console.Write(message);
            }

            public void WriteLine(string message)
            {
                System.Console.WriteLine(message);
            }
        }
    }
}