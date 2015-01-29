// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.Framework.Logging
{
    /// <summary>
    /// LoggerStructure to enable formatting options supported by <see cref="string.Format"/>. 
    /// This also enables using {NamedformatItem} in the format string.
    /// </summary>
    public class LoggerStructureFormat : ILoggerStructure
    {
        private static ConcurrentDictionary<string, Formatter> _formatters = new ConcurrentDictionary<string, Formatter>();
        private readonly Formatter _formatter;
        private readonly object[] _values;

        public LoggerStructureFormat(string format, object[] values)
        {
            _formatter = _formatters.GetOrAdd(format, f => new Formatter(f));
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

        private class Formatter
        {
            private readonly string _format;
            private readonly List<string> _valueNames = new List<string>();

            public Formatter(string format)
            {
                var sb = new StringBuilder();
                var scanIndex = 0;
                var endIndex = format.Length;

                while (scanIndex < endIndex)
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

            private static int FindIndexOf(string format, char ch, int startIndex, int endIndex)
            {
                var findIndex = format.IndexOf(ch, startIndex, endIndex - startIndex);
                return findIndex == -1 ? endIndex : findIndex;
            }

            public string Format(object[] values)
            {
                return string.Format(CultureInfo.InvariantCulture, _format, values);
            }

            public IEnumerable<KeyValuePair<string, object>> GetValues(object[] values)
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