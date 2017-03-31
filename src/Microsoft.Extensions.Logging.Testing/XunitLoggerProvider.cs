// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging.Testing
{
    public class XunitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _output;
        private readonly LogLevel _minLevel;

        public XunitLoggerProvider(ITestOutputHelper output)
            : this(output, LogLevel.Trace)
        {
        }

        public XunitLoggerProvider(ITestOutputHelper output, LogLevel minLevel)
        {
            _output = output;
            _minLevel = minLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XunitLogger(_output, categoryName, _minLevel);
        }

        public void Dispose()
        {
        }
    }

    public class XunitLogger : ILogger
    {
        private static readonly char[] NewLineChars = new[] { '\r', '\n' };
        private readonly string _category;
        private readonly LogLevel _minLogLevel;
        private readonly ITestOutputHelper _output;

        public XunitLogger(ITestOutputHelper output, string category, LogLevel minLogLevel)
        {
            _minLogLevel = minLogLevel;
            _category = category;
            _output = output;
        }

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            var firstLinePrefix = $"| {_category} {logLevel}: ";
            var lines = formatter(state, exception).Split('\n');
            WriteLine(firstLinePrefix + lines.First().TrimEnd(NewLineChars));

            var additionalLinePrefix = "|" + new string(' ', firstLinePrefix.Length - 1);
            foreach (var line in lines.Skip(1))
            {
                WriteLine(additionalLinePrefix + line.TrimEnd(NewLineChars));
            }
        }

        public bool IsEnabled(LogLevel logLevel)
            => logLevel >= _minLogLevel;

        public IDisposable BeginScope<TState>(TState state)
            => new NullScope();

        private void WriteLine(string message)
        {
            try
            {
                _output.WriteLine(message);
            }
            catch (Exception)
            {
                // We could fail because we're on a background thread and our captured ITestOutputHelper is
                // busted (if the test "completed" before the background thread fired).
                // So, ignore this. There isn't really anything we can do but hope the
                // caller has additional loggers registered
            }
        }

        private class NullScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
