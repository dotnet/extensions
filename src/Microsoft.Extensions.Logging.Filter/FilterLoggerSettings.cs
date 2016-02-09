using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging.Filter
{
    public class FilterLoggerSettings : IFilterLoggerSettings, IEnumerable<KeyValuePair<string, LogLevel>>
    {
        //TODO
        IChangeToken IFilterLoggerSettings.ChangeToken => null;

        public IDictionary<string, LogLevel> Switches { get; set; } = new Dictionary<string, LogLevel>();

        public void Add(string categoryName, LogLevel logLevel) => Switches.Add(categoryName, logLevel);

        IFilterLoggerSettings IFilterLoggerSettings.Reload() => this;

        public bool TryGetSwitch(string name, out LogLevel level) => Switches.TryGetValue(name, out level);

        IEnumerator<KeyValuePair<string, LogLevel>> IEnumerable<KeyValuePair<string, LogLevel>>.GetEnumerator() => Switches.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Switches.GetEnumerator();
    }
}
