using Microsoft.Framework.Logging.Internal;
using System;
using System.Collections.Generic;

namespace Microsoft.Framework.Logging
{
    public static class LoggerMessage
    {
        public static void DefineScope(out Func<ILogger, IDisposable> scope, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            scope = logger => logger.BeginScopeImpl(new LogValues(formatter));
        }

        public static void DefineScope<T1>(out Func<ILogger, T1, IDisposable> scope, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            scope = (logger, arg1) => logger.BeginScopeImpl(new LogValues<T1>(formatter, arg1));
        }

        public static void DefineScope<T1, T2>(out Func<ILogger, T1, T2, IDisposable> scope, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            scope = (logger, arg1, arg2) => logger.BeginScopeImpl(new LogValues<T1, T2>(formatter, arg1, arg2));
        }

        public static void DefineScope<T1, T2, T3>(out Func<ILogger, T1, T2, T3, IDisposable> scope, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            scope = (logger, arg1, arg2, arg3) => logger.BeginScopeImpl(new LogValues<T1, T2, T3>(formatter, arg1, arg2, arg3));
        }

        public static void Define<T1>(out Action<ILogger, T1, Exception> message, LogLevel logLevel, int eventId, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            message = (logger, arg1, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<T1>(formatter, arg1), exception, LogValues<T1>.Callback);
                }
            };
        }

        public static void Define<T1, T2>(out Action<ILogger, T1, T2, Exception> message, LogLevel logLevel, int eventId, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            message = (logger, arg1, arg2, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<T1, T2>(formatter, arg1, arg2), exception, LogValues<T1, T2>.Callback);
                }
            };
        }


        public static void Define<T1, T2, T3>(out Action<ILogger, T1, T2, T3, Exception> message, LogLevel logLevel, int eventId, string formatString)
        {
            var formatter = new LogValuesFormatter(formatString);

            message = (logger, arg1, arg2, arg3, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<T1, T2, T3>(formatter, arg1, arg2, arg3), exception, LogValues<T1, T2, T3>.Callback);
                }
            };
        }

        public static void Define<T1>(out Action<ILogger, T1, Exception> message, LogLevel logLevel, int eventId, string eventName, string formatString)
        {
            var formatter = new LogValuesFormatter("{EventName}: " + formatString);
            Func<object, Exception, string> callback = (state, error) => formatter.Format(((LogValues<string, T1>)state).ToArray());

            message = (logger, arg1, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<string, T1>(formatter, eventName, arg1), exception, LogValues<string, T1>.Callback);
                }
            };
        }

        public static void Define<T1, T2>(out Action<ILogger, T1, T2, Exception> message, LogLevel logLevel, int eventId, string eventName, string formatString)
        {
            var formatter = new LogValuesFormatter("{EventName}: " + formatString);
            Func<object, Exception, string> callback = (state, error) => formatter.Format(((LogValues<string, T1, T2>)state).ToArray());

            message = (logger, arg1, arg2, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<string, T1, T2>(formatter, eventName, arg1, arg2), exception, LogValues<string, T1, T2>.Callback);
                }
            };
        }

        private class LogValues : ILogValues
        {
            public static Func<object, Exception, string> Callback = (state, exception) => ((LogValues)state)._formatter.Format(((LogValues)state).ToArray());

            private static IEnumerable<KeyValuePair<string, object>> _getValues = new KeyValuePair<string, object>[0];
            private static object[] _toArray = new object[0];

            private readonly LogValuesFormatter _formatter;

            public LogValues(LogValuesFormatter formatter)
            {
                _formatter = formatter;
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues() => _getValues;

            public object[] ToArray() => _toArray;

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
    }
}

