// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Extensions.Logging.Internal
{
    /// <summary>
    /// LogValues to enable formatting options supported by <see cref="string.Format"/>. 
    /// This also enables using {NamedformatItem} in the format string.
    /// </summary>
    public class FormattedLogValues : IReadOnlyList<KeyValuePair<string, object>>
    {
        private static ConcurrentDictionary<string, LogValuesFormatter> _formatters = new ConcurrentDictionary<string, LogValuesFormatter>();
        private readonly LogValuesFormatter _formatter;
        private readonly object[] _values;

        public FormattedLogValues(string format, params object[] values)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }
            _formatter = _formatters.GetOrAdd(format, f => new LogValuesFormatter(f));
            _values = values;
        }

        public KeyValuePair<string, object> this[int index]
        {
            get
            {
                if (index > Count)
                {
                    throw new IndexOutOfRangeException(nameof(index));
                }
                return _formatter.GetValue(_values, index);
            }
        }

        public int Count
        {
            get
            {
                return _formatter.ValueNames.Count + 1;
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
            {
                yield return this[i];
            }
        }

        public override string ToString()
        {
            return _formatter.Format(_values);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
