// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.Test
{
    public class TestLogger : ILogger
    {
        private object _scope;
        private readonly TestSink _sink;
        private readonly string _name;
        private readonly Func<LogLevel, bool> _filter;

        public TestLogger(string name, TestSink sink, bool enabled) :
            this(name, sink, _ => enabled)
        {
        }

        public TestLogger(string name, TestSink sink, Func<LogLevel, bool> filter)
        {
            _sink = sink;
            _name = name;
            _filter = filter;
        }

        public string Name { get; set; }

        public IDisposable BeginScope<TState>(TState state)
        {
            _scope = state;

            _sink.Begin(new BeginScopeContext()
            {
                LoggerName = _name,
                Scope = state,
            });

            return NoopDisposable.Instance;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            _sink.Write(new WriteContext()
            {
                LogLevel = logLevel,
                EventId = eventId,
                State = state,
                Exception = exception,
                Formatter = (s, e) => formatter((TState)s, e),
                LoggerName = _name,
                Scope = _scope
            });
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _filter(logLevel);
        }

        private class NoopDisposable : IDisposable
        {
            public static NoopDisposable Instance = new NoopDisposable();

            public void Dispose()
            {
            }
        }
    }
}