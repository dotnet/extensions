// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Internal;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Creates delegates which can be later cached to log messages in a performant way.
    /// </summary>
    public static class LoggerMessage
    {
        /// <summary>
        /// Creates a delegate which can be invoked to create a log scope.
        /// </summary>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log scope.</returns>
        public static Func<ILogger, IDisposable> DefineScope(string formatString)
        {
            var logValues = new LogValues(new LogValuesFormatter(formatString));

            return logger => logger.BeginScopeImpl(logValues);
        }

        /// <summary>
        /// Creates a delegate which can be invoked to create a log scope.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log scope.</returns>
        public static Func<ILogger, T1, IDisposable> DefineScope<T1>(string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            return (logger, arg1) => logger.BeginScopeImpl(new LogValues<T1>(formatter, arg1));
        }

        /// <summary>
        /// Creates a delegate which can be invoked to create a log scope.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log scope.</returns>
        public static Func<ILogger, T1, T2, IDisposable> DefineScope<T1, T2>(string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            return (logger, arg1, arg2) => logger.BeginScopeImpl(new LogValues<T1, T2>(formatter, arg1, arg2));
        }

        /// <summary>
        /// Creates a delegate which can be invoked to create a log scope.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log scope.</returns>
        public static Func<ILogger, T1, T2, T3, IDisposable> DefineScope<T1, T2, T3>(string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            return (logger, arg1, arg2, arg3) => logger.BeginScopeImpl(new LogValues<T1, T2, T3>(formatter, arg1, arg2, arg3));
        }

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, Exception> Define(LogLevel logLevel, int eventId, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            return (logger, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues(formatter), exception, LogValues.Callback);
                }
            };
        }

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, Exception> Define<T1>(LogLevel logLevel, int eventId, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            return (logger, arg1, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<T1>(formatter, arg1), exception, LogValues<T1>.Callback);
                }
            };
        }

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, T2, Exception> Define<T1, T2>(LogLevel logLevel, int eventId, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            return (logger, arg1, arg2, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<T1, T2>(formatter, arg1, arg2), exception, LogValues<T1, T2>.Callback);
                }
            };
        }

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, T2, T3, Exception> Define<T1, T2, T3>(LogLevel logLevel, int eventId, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            return (logger, arg1, arg2, arg3, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<T1, T2, T3>(formatter, arg1, arg2, arg3), exception, LogValues<T1, T2, T3>.Callback);
                }
            };
        }

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, T2, T3, T4, Exception> Define<T1, T2, T3, T4>(LogLevel logLevel, int eventId, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            return (logger, arg1, arg2, arg3, arg4, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<T1, T2, T3, T4>(formatter, arg1, arg2, arg3, arg4), exception, LogValues<T1, T2, T3, T4>.Callback);
                }
            };
        }

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter passed to the named format string.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, T2, T3, T4, T5, Exception> Define<T1, T2, T3, T4, T5>(LogLevel logLevel, int eventId, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            return (logger, arg1, arg2, arg3, arg4, arg5, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<T1, T2, T3, T4, T5>(formatter, arg1, arg2, arg3, arg4, arg5), exception, LogValues<T1, T2, T3, T4, T5>.Callback);
                }
            };
        }

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter passed to the named format string.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter passed to the named format string.</typeparam>
        /// <typeparam name="T5">The type of the sixth parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, T2, T3, T4, T5, T6, Exception> Define<T1, T2, T3, T4, T5, T6>(LogLevel logLevel, int eventId, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            return (logger, arg1, arg2, arg3, arg4, arg5, arg6, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<T1, T2, T3, T4, T5, T6>(formatter, arg1, arg2, arg3, arg4, arg5, arg6), exception, LogValues<T1, T2, T3, T4, T5, T6>.Callback);
                }
            };
        }

        private class LogValues : ILogValues
        {
            public static Func<object, Exception, string> Callback = (state, exception) => ((LogValues)state)._formatter.Format(((LogValues)state).ToArray());

            private static object[] _valueArray = new object[0];

            private readonly LogValuesFormatter _formatter;

            public LogValues(LogValuesFormatter formatter)
            {
                _formatter = formatter;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues() => new[]
            {
                new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat),
            };

            public object[] ToArray() => _valueArray;

            public override string ToString() => _formatter.Format(ToArray());
        }

        private class LogValues<T0> : ILogValues
        {
            public static Func<object, Exception, string> Callback = (state, exception) => ((LogValues<T0>)state)._formatter.Format(((LogValues<T0>)state).ToArray());

            private readonly LogValuesFormatter _formatter;
            private readonly T0 _value0;

            public LogValues(LogValuesFormatter formatter, T0 value0)
            {
                _formatter = formatter;
                _value0 = value0;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues() => new[]
            {
                new KeyValuePair<string, object>(_formatter.ValueNames[0], _value0),
                new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat),
            };

            public object[] ToArray() => new object[] { _value0 };

            public override string ToString() => _formatter.Format(ToArray());
        }

        private class LogValues<T0, T1> : ILogValues
        {
            public static Func<object, Exception, string> Callback = (state, exception) => ((LogValues<T0, T1>)state)._formatter.Format(((LogValues<T0, T1>)state).ToArray());

            private readonly LogValuesFormatter _formatter;
            private readonly T0 _value0;
            private readonly T1 _value1;

            public LogValues(LogValuesFormatter formatter, T0 value0, T1 value1)
            {
                _formatter = formatter;
                _value0 = value0;
                _value1 = value1;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues() => new[]
            {
                new KeyValuePair<string, object>(_formatter.ValueNames[0], _value0),
                new KeyValuePair<string, object>(_formatter.ValueNames[1], _value1),
                new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat),
            };

            public object[] ToArray() => new object[] { _value0, _value1 };

            public override string ToString() => _formatter.Format(ToArray());
        }

        private class LogValues<T0, T1, T2> : ILogValues
        {
            public static Func<object, Exception, string> Callback = (state, exception) => ((LogValues<T0, T1, T2>)state)._formatter.Format(((LogValues<T0, T1, T2>)state).ToArray());

            private readonly LogValuesFormatter _formatter;
            public T0 _value0;
            public T1 _value1;
            public T2 _value2;

            public LogValues(LogValuesFormatter formatter, T0 value0, T1 value1, T2 value2)
            {
                _formatter = formatter;
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues() => new[]
            {
                new KeyValuePair<string, object>(_formatter.ValueNames[0], _value0),
                new KeyValuePair<string, object>(_formatter.ValueNames[1], _value1),
                new KeyValuePair<string, object>(_formatter.ValueNames[2], _value2),
                new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat),
            };

            public object[] ToArray() => new object[] { _value0, _value1, _value2 };

            public override string ToString() => _formatter.Format(ToArray());
        }

        private class LogValues<T0, T1, T2, T3> : ILogValues
        {
            public static Func<object, Exception, string> Callback = (state, exception) => ((LogValues<T0, T1, T2, T3>)state)._formatter.Format(((LogValues<T0, T1, T2, T3>)state).ToArray());

            private readonly LogValuesFormatter _formatter;
            public T0 _value0;
            public T1 _value1;
            public T2 _value2;
            public T3 _value3;

            public LogValues(LogValuesFormatter formatter, T0 value0, T1 value1, T2 value2, T3 value3)
            {
                _formatter = formatter;
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
                _value3 = value3;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues() => new[]
            {
                new KeyValuePair<string, object>(_formatter.ValueNames[0], _value0),
                new KeyValuePair<string, object>(_formatter.ValueNames[1], _value1),
                new KeyValuePair<string, object>(_formatter.ValueNames[2], _value2),
                new KeyValuePair<string, object>(_formatter.ValueNames[3], _value3),
                new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat),
            };

            public object[] ToArray() => new object[] { _value0, _value1, _value2, _value3 };

            public override string ToString() => _formatter.Format(ToArray());
        }

        private class LogValues<T0, T1, T2, T3, T4> : ILogValues
        {
            public static Func<object, Exception, string> Callback = (state, exception) => ((LogValues<T0, T1, T2, T3, T4>)state)._formatter.Format(((LogValues<T0, T1, T2, T3, T4>)state).ToArray());

            private readonly LogValuesFormatter _formatter;
            public T0 _value0;
            public T1 _value1;
            public T2 _value2;
            public T3 _value3;
            public T4 _value4;

            public LogValues(LogValuesFormatter formatter, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4)
            {
                _formatter = formatter;
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
                _value3 = value3;
                _value4 = value4;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues() => new[]
            {
                new KeyValuePair<string, object>(_formatter.ValueNames[0], _value0),
                new KeyValuePair<string, object>(_formatter.ValueNames[1], _value1),
                new KeyValuePair<string, object>(_formatter.ValueNames[2], _value2),
                new KeyValuePair<string, object>(_formatter.ValueNames[3], _value3),
                new KeyValuePair<string, object>(_formatter.ValueNames[4], _value4),
                new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat),
            };

            public object[] ToArray() => new object[] { _value0, _value1, _value2, _value3, _value4 };

            public override string ToString() => _formatter.Format(ToArray());
        }

        private class LogValues<T0, T1, T2, T3, T4, T5> : ILogValues
        {
            public static Func<object, Exception, string> Callback = (state, exception) => ((LogValues<T0, T1, T2, T3, T4, T5>)state)._formatter.Format(((LogValues<T0, T1, T2, T3, T4, T5>)state).ToArray());

            private readonly LogValuesFormatter _formatter;
            public T0 _value0;
            public T1 _value1;
            public T2 _value2;
            public T3 _value3;
            public T4 _value4;
            public T5 _value5;

            public LogValues(LogValuesFormatter formatter, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
            {
                _formatter = formatter;
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
                _value3 = value3;
                _value4 = value4;
                _value5 = value5;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues() => new[]
            {
                new KeyValuePair<string, object>(_formatter.ValueNames[0], _value0),
                new KeyValuePair<string, object>(_formatter.ValueNames[1], _value1),
                new KeyValuePair<string, object>(_formatter.ValueNames[2], _value2),
                new KeyValuePair<string, object>(_formatter.ValueNames[3], _value3),
                new KeyValuePair<string, object>(_formatter.ValueNames[4], _value4),
                new KeyValuePair<string, object>(_formatter.ValueNames[5], _value5),
                new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat),
            };

            public object[] ToArray() => new object[] { _value0, _value1, _value2, _value3, _value4, _value5 };

            public override string ToString() => _formatter.Format(ToArray());
        }

        private class LogValues<T0, T1, T2, T3, T4, T5, T6> : ILogValues
        {
            public static Func<object, Exception, string> Callback = (state, exception) => ((LogValues<T0, T1, T2, T3, T4, T5, T6>)state)._formatter.Format(((LogValues<T0, T1, T2, T3, T4, T5, T6>)state).ToArray());

            private readonly LogValuesFormatter _formatter;
            public T0 _value0;
            public T1 _value1;
            public T2 _value2;
            public T3 _value3;
            public T4 _value4;
            public T5 _value5;
            public T6 _value6;

            public LogValues(LogValuesFormatter formatter, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
            {
                _formatter = formatter;
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
                _value3 = value3;
                _value4 = value4;
                _value5 = value5;
                _value6 = value6;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues() => new[]
            {
                new KeyValuePair<string, object>(_formatter.ValueNames[0], _value0),
                new KeyValuePair<string, object>(_formatter.ValueNames[1], _value1),
                new KeyValuePair<string, object>(_formatter.ValueNames[2], _value2),
                new KeyValuePair<string, object>(_formatter.ValueNames[3], _value3),
                new KeyValuePair<string, object>(_formatter.ValueNames[4], _value4),
                new KeyValuePair<string, object>(_formatter.ValueNames[5], _value5),
                new KeyValuePair<string, object>(_formatter.ValueNames[6], _value6),
                new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat),
            };

            public object[] ToArray() => new object[] { _value0, _value1, _value2, _value3, _value4, _value5, _value6 };

            public override string ToString() => _formatter.Format(ToArray());
        }
    }
}

