using Microsoft.Framework.Logging.Internal;
using System;
using System.Collections.Generic;

namespace Microsoft.Framework.Logging
{
    public static class LoggerMessage
    {
        public static void Define<T1>(out Action<ILogger, T1, Exception> message, LogLevel logLevel, int eventId, string eventName, string formatString)
        {
            var formatter = new LogValuesFormatter("{EventName}: " + formatString);
            Func<object, Exception, string> callback = (state, error) => formatter.Format(((LogValues<string, T1>)state).ToArray());

            message = (logger, arg1, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<string, T1> { _valueNames = formatter.ValueNames, _value0 = eventName, _value1 = arg1 }, exception, callback);
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
                    logger.Log(logLevel, eventId, new LogValues<string, T1, T2> { _valueNames = formatter.ValueNames, _value0 = eventName, _value1 = arg1, _value2 = arg2 }, exception, callback);
                }
            };
        }

        private class LogValues<T0, T1> : ILogValues
        {
            public IList<string> _valueNames;
            public T0 _value0;
            public T1 _value1;

            public IEnumerable<KeyValuePair<string, object>> GetValues() => new[]
            {
                new KeyValuePair<string, object>(_valueNames[0], _value0),
                new KeyValuePair<string, object>(_valueNames[1], _value1),
            };

            public object[] ToArray() => new object[] { _value0, _value1 };

            public string Format()
            {
                return $"{_valueNames[0]}:{_value0} {_valueNames[1]}:{_value1}";
            }
        }

        private class LogValues<T0, T1, T2> : ILogValues
        {
            public IList<string> _valueNames;
            public T0 _value0;
            public T1 _value1;
            public T2 _value2;

            public IEnumerable<KeyValuePair<string, object>> GetValues()
            {
                return new[]
                {
                    new KeyValuePair<string, object>(_valueNames[0], _value0),
                    new KeyValuePair<string, object>(_valueNames[1], _value1),
                    new KeyValuePair<string, object>(_valueNames[2], _value2),
                };
            }

            public object[] ToArray()
            {
                return new object[] { _value0, _value1, _value2 };
            }

            public string Format()
            {
                return $"{_valueNames[0]}:{_value0} {_valueNames[1]}:{_value1} {_valueNames[2]}:{_value2}";
            }
        }
    }
}

