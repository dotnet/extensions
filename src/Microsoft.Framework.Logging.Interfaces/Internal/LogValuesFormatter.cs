// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.Framework.Logging.Internal
{
    /// <summary>
    /// Formatter to convert the named format items like {NamedformatItem} to <see cref="string.Format"/> format.
    /// </summary>
    public class LogValuesFormatter
    {
        private readonly string _format;
        private readonly List<string> _valueNames = new List<string>();

        public string OriginalFormat { get; private set; }

        public LogValuesFormatter(string format)
        {
            OriginalFormat = format;

            var sb = new StringBuilder();
            var scanIndex = 0;
            var endIndex = format.Length;

            while (scanIndex < endIndex)
            {
                var openBraceIndex = FindBraceIndex(format, '{', scanIndex, endIndex);
                var closeBraceIndex = FindBraceIndex(format, '}', openBraceIndex, endIndex);

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

        private static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
        {
            // Example: {{prefix{{{Argument}}}suffix}}.
            var braceIndex = endIndex;
            var scanIndex = startIndex;
            var braceOccurenceCount = 0;

            while (scanIndex < endIndex)
            {
                if (braceOccurenceCount > 0 && format[scanIndex] != brace)
                {
                    if (braceOccurenceCount % 2 == 0)
                    {
                        // Even number of '{' or '}' found. Proceed search with next occurence of '{' or '}'.
                        braceOccurenceCount = 0;
                        braceIndex = endIndex;
                    }
                    else
                    {
                        // An unescaped '{' or '}' found.
                        break;
                    }
                }
                else if (format[scanIndex] == brace)
                {
                    if (brace == '}')
                    {
                        if (braceOccurenceCount == 0)
                        {
                            // For '}' pick the first occurence.
                            braceIndex = scanIndex;
                        }
                    }
                    else
                    {
                        // For '{' pick the last occurence.
                        braceIndex = scanIndex;
                    }

                    braceOccurenceCount++;
                }

                scanIndex++;
            }

            return braceIndex;
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
            var valueArray = new KeyValuePair<string, object>[values.Length + 1];
            for (var index = 0; index != _valueNames.Count; ++index)
            {
                valueArray[index] = new KeyValuePair<string, object>(_valueNames[index], values[index]);
            }

            valueArray[valueArray.Length - 1] = new KeyValuePair<string, object>("{OriginalFormat}", OriginalFormat);
            return valueArray;
        }
    }
}
