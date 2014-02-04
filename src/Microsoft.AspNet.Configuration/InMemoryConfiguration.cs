using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Configuration
{
    public class InMemoryConfiguration : IConfiguration
    {
        private IDictionary<string, string> _data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public InMemoryConfiguration()
        {
        }

        public InMemoryConfiguration(IEnumerable<KeyValuePair<string, string>> initialData)
        {
            foreach (var pair in initialData)
            {
                _data.Add(pair);
            }
        }

        public string this[string key]
        {
            get { return Get(key); }
            set  { _data[key] = value; }
        }

        public string Get(string key)
        {
            string value;
            if (_data.TryGetValue(key, out value))
            {
                return value;
            }
            return null;
        }
    }
}
