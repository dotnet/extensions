using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.Framework.Logging
{
    public class LoggerStructureFormat : ILoggerStructure
    {
        static Dictionary<string, Formatter> _formatters = new Dictionary<string, Formatter>();
        static object _formattersLock = new Object();
        private readonly Formatter _formatter;
        private readonly object[] _values;

        public LoggerStructureFormat(string format, params object[] values)
        {
            lock (_formattersLock)
            {
                // TODO: ConcurrentDictionary not available in portable profile?
                // _formatter = _formatters.GetOrAdd(format, ParseFormatter);
                if (!_formatters.TryGetValue(format, out _formatter))
                {
                    _formatter = ParseFormatter(format);
                    _formatters[format] = _formatter;
                }
            }
            _values = values;
        }

        public string Message { get { return null; } }

        public string Format()
        {
            return _formatter.Format(_values);
        }

        public IEnumerable<KeyValuePair<string, object>> GetValues()
        {
            return _formatter.GetValues(_values);
        }

        private Formatter ParseFormatter(string format)
        {
            return new Formatter(format);
        }

        class Formatter
        {
            private readonly string _format;
            private readonly List<string> _valueNames = new List<string>();

            public Formatter(string format)
            {
                var sb = new StringBuilder();
                var endIndex = format.Length;
                for (var scanIndex = 0; scanIndex != endIndex;)
                {
                    var openBraceIndex = FindIndexOf(format, '{', scanIndex, endIndex);
                    var closeBraceIndex = FindIndexOf(format, '}', openBraceIndex, endIndex);

                    // Format item syntax : { index[,alignment][ :formatString] }.
                    var formatDelimiterIndex = FindIndexOf(format, ',', openBraceIndex, closeBraceIndex);
                    if (formatDelimiterIndex == closeBraceIndex)
                    {
                        formatDelimiterIndex = FindIndexOf(format, ':', openBraceIndex, closeBraceIndex);
                    }

                    if (closeBraceIndex == endIndex)
                    {
                        sb.Append(format, scanIndex, endIndex - scanIndex);
                        scanIndex = endIndex;
                    }
                    else
                    {
                        sb.Append(format, scanIndex, openBraceIndex - scanIndex + 1);
                        sb.Append(_valueNames.Count.ToString(CultureInfo.InvariantCulture));
                        _valueNames.Add(format.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1));
                        sb.Append(format, formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1);

                        scanIndex = closeBraceIndex + 1;
                    }
                }
                _format = sb.ToString();
            }

            int FindIndexOf(string format, char ch, int startIndex, int endIndex)
            {
                var findIndex = format.IndexOf(ch, startIndex, endIndex - startIndex);
                return findIndex == -1 ? endIndex : findIndex;
            }

            internal string Format(object[] values)
            {
                return string.Format(CultureInfo.InvariantCulture, _format, values);
            }

            internal IEnumerable<KeyValuePair<string, object>> GetValues(object[] values)
            {
                var valueArray = new KeyValuePair<string, object>[values.Length];
                for (var index = 0; index != values.Length; ++index)
                {
                    valueArray[index] = new KeyValuePair<string, object>(_valueNames[index], values[index]);
                }
                return valueArray;
            }
        }
    }
}
