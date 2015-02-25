// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Framework.Logging.Internal;

namespace Microsoft.Framework.Logging
{
    /// <summary>
    /// LoggerStructure to enable formatting options supported by <see cref="string.Format"/>. 
    /// This also enables using {NamedformatItem} in the format string.
    /// </summary>
    public class LoggerStructureFormat : ILoggerStructure
    {
        private static ConcurrentDictionary<string, LoggerStructureFormatter> _formatters = new ConcurrentDictionary<string, LoggerStructureFormatter>();
        private readonly LoggerStructureFormatter _formatter;
        private readonly object[] _values;

        public LoggerStructureFormat(string format, params object[] values)
        {
            _formatter = _formatters.GetOrAdd(format, f => new LoggerStructureFormatter(f));
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
    }
}