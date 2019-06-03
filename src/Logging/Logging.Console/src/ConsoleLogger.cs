// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.Extensions.Logging.Console
{
    internal class ConsoleLogger : ILogger
    {
        private static readonly string _loglevelPadding = ": ";
        private static readonly string _messagePadding;
        private static readonly string _newLineWithMessagePadding;

        // ConsoleColor does not have a value to specify the 'Default' color
        private readonly ConsoleColor? DefaultConsoleColor = null;

        private readonly string _name;
        private readonly ConsoleLoggerProcessor _queueProcessor;

        [ThreadStatic]
        private static StringBuilder _logBuilder;

        static ConsoleLogger()
        {
            var logLevelString = GetLogLevelString(LogLevel.Information);
            _messagePadding = new string(' ', logLevelString.Length + _loglevelPadding.Length);
            _newLineWithMessagePadding = Environment.NewLine + _messagePadding;
        }

        internal ConsoleLogger(string name, ConsoleLoggerProcessor loggerProcessor)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            _name = name;
            _queueProcessor = loggerProcessor;
        }

        internal IExternalScopeProvider ScopeProvider { get; set; }

        internal ConsoleLoggerOptions Options { get; set; }

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

            var message = formatter(state, exception);

            if (!string.IsNullOrEmpty(message) || exception != null)
            {
                WriteMessage(logLevel, _name, eventId.Id, message, exception);
            }
        }

        public virtual void WriteMessage(LogLevel logLevel, string logName, int eventId, string message, Exception exception)
        {
            var logBuilder = _logBuilder;
            _logBuilder = null;

            if (logBuilder == null)
            {
                logBuilder = new StringBuilder();
            }

            if (Options.Format == ConsoleLoggerFormat.Default)
            {
                // Example:
                // INFO: ConsoleApp.Program[10]
                //       Request received

                var logLevelColors = GetLogLevelConsoleColors(logLevel);
                var logLevelString = GetLogLevelString(logLevel);
                // category and event id
                logBuilder.Append(_loglevelPadding);
                logBuilder.Append(logName);
                logBuilder.Append("[");
                logBuilder.Append(eventId);
                logBuilder.AppendLine("]");

                // scope information
                GetScopeInformation(logBuilder, multiLine: true);

                if (!string.IsNullOrEmpty(message))
                {
                    // message
                    logBuilder.Append(_messagePadding);

                    var len = logBuilder.Length;
                    logBuilder.AppendLine(message);
                    logBuilder.Replace(Environment.NewLine, _newLineWithMessagePadding, len, message.Length);
                }

                // Example:
                // System.InvalidOperationException
                //    at Namespace.Class.Function() in File:line X
                if (exception != null)
                {
                    // exception message
                    logBuilder.AppendLine(exception.ToString());
                }

                var timestampFormat = Options.TimestampFormat;
                // Queue log message
                _queueProcessor.EnqueueMessage(new LogMessageEntry(
                    message: logBuilder.ToString(),
                    timeStamp: timestampFormat != null ? DateTime.Now.ToString(timestampFormat) : null,
                    levelString: logLevelString,
                    levelBackground: logLevelColors.Background,
                    levelForeground: logLevelColors.Foreground,
                    messageColor: DefaultConsoleColor,
                    logAsError: logLevel >= Options.LogToStandardErrorThreshold
                ));
            }
            else if (Options.Format == ConsoleLoggerFormat.Systemd)
            {
                // systemd reads messages from standard out line-by-line in a '<pri>message' format.
                // newline characters are treated as message delimiters, so we must replace them.
                // Messages longer than the journal LineMax setting (default: 48KB) are cropped.
                // Example:
                // <6>ConsoleApp.Program[10] Request received

                // loglevel
                var logLevelString = GetSyslogSeverityString(logLevel);
                logBuilder.Append(logLevelString);

                // timestamp
                var timestampFormat = Options.TimestampFormat;
                if (timestampFormat != null)
                {
                    logBuilder.Append(DateTime.Now.ToString(timestampFormat));
                }

                // category and event id
                logBuilder.Append(logName);
                logBuilder.Append("[");
                logBuilder.Append(eventId);
                logBuilder.Append("]");

                // scope information
                GetScopeInformation(logBuilder, multiLine: false);

                // message
                if (!string.IsNullOrEmpty(message))
                {
                    logBuilder.Append(' ');
                    // message
                    var len = logBuilder.Length;
                    AppendAndReplaceNewLine(logBuilder, message);
                }

                // exception
                // System.InvalidOperationException at Namespace.Class.Function() in File:line X
                if (exception != null)
                {
                    logBuilder.Append(' ');
                    AppendAndReplaceNewLine(logBuilder, exception.ToString());
                }

                // newline delimiter
                logBuilder.Append(Environment.NewLine);

                // Queue log message
                _queueProcessor.EnqueueMessage(new LogMessageEntry(
                    message: logBuilder.ToString()
                ));

                void AppendAndReplaceNewLine(StringBuilder sb, string message)
                {
                    var len = sb.Length;
                    sb.Append(message);
                    sb.Replace(Environment.NewLine, " ", len, message.Length);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(Options.Format));
            }

            logBuilder.Clear();
            if (logBuilder.Capacity > 1024)
            {
                logBuilder.Capacity = 1024;
            }
            _logBuilder = logBuilder;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public IDisposable BeginScope<TState>(TState state) => ScopeProvider?.Push(state) ?? NullScope.Instance;

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

        private static string GetSyslogSeverityString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    return "<7>";
                case LogLevel.Information:
                    return "<6>";
                case LogLevel.Warning:
                    return "<4>";
                case LogLevel.Error:
                    return "<3>";
                case LogLevel.Critical:
                    return "<2>";
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
        {
            if (Options.DisableColors)
            {
                return new ConsoleColors(null, null);
            }

            // We must explicitly set the background color if we are setting the foreground color,
            // since just setting one can look bad on the users console.
            switch (logLevel)
            {
                case LogLevel.Critical:
                    return new ConsoleColors(ConsoleColor.White, ConsoleColor.Red);
                case LogLevel.Error:
                    return new ConsoleColors(ConsoleColor.Black, ConsoleColor.Red);
                case LogLevel.Warning:
                    return new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black);
                case LogLevel.Information:
                    return new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black);
                case LogLevel.Debug:
                    return new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black);
                case LogLevel.Trace:
                    return new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black);
                default:
                    return new ConsoleColors(DefaultConsoleColor, DefaultConsoleColor);
            }
        }

        private void GetScopeInformation(StringBuilder stringBuilder, bool multiLine)
        {
            var scopeProvider = ScopeProvider;
            if (Options.IncludeScopes && scopeProvider != null)
            {
                var initialLength = stringBuilder.Length;

                scopeProvider.ForEachScope((scope, state) =>
                {
                    var (builder, paddAt) = state;
                    var padd = paddAt == builder.Length;
                    if (padd)
                    {
                        builder.Append(_messagePadding);
                        builder.Append("=> ");
                    }
                    else
                    {
                        builder.Append(" => ");
                    }
                    builder.Append(scope);
                }, (stringBuilder, multiLine ? initialLength : -1));

                if (stringBuilder.Length > initialLength && multiLine)
                {
                    stringBuilder.AppendLine();
                }
            }
        }

        private readonly struct ConsoleColors
        {
            public ConsoleColors(ConsoleColor? foreground, ConsoleColor? background)
            {
                Foreground = foreground;
                Background = background;
            }

            public ConsoleColor? Foreground { get; }

            public ConsoleColor? Background { get; }
        }
    }
}
