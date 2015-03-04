// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Framework.Logging.Internal
{
    /// <summary>
    /// LogValues to enable formatting options supported by <see cref="string.Format"/>. 
    /// This also enables using {NamedformatItem} in the format string.
    /// </summary>
    public class FormattedLogValues : ILogValues
    {
        private static ConcurrentDictionary<string, LogValuesFormatter> _formatters = new ConcurrentDictionary<string, LogValuesFormatter>();
        private readonly LogValuesFormatter _formatter;
        private readonly object[] _values;

        public FormattedLogValues(string format, params object[] values)
        {
            _formatter = _formatters.GetOrAdd(format, f => new LogValuesFormatter(f));
            _values = values;
        }

        public string Format()
        {
            return _formatter.Format(_values);
        }

        public IEnumerable<KeyValuePair<string, object>> GetValues()
        {
            return _formatter.GetValues(_values);
        }
    }
}
