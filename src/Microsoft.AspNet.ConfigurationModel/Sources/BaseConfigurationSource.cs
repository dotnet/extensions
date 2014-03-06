using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    public abstract class BaseConfigurationSource : IConfigurationSource
    {
        public IDictionary<string, string> Data { get; private set; }

        protected BaseConfigurationSource()
        {
            Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        protected virtual void ReplaceData(Dictionary<string, string> data)
        {
            Data = data;
        }

        public virtual bool TryGet(string key, out string value)
        {
            return Data.TryGetValue(key, out value);
        }

        public virtual void Set(string key, string value)
        {
            Data[key] = value;
        }

        public virtual void Load()
        {
        }
       
        public virtual IEnumerable<string> ProduceSubKeys(IEnumerable<string> earlierKeys, string prefix, string delimiter)
        {
            return Data
                .Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(kv => Segment(kv.Key, prefix, delimiter))
                .Concat(earlierKeys);
        }

        private static string Segment(string key, string prefix, string delimiter)
        {
            var indexOf = key.IndexOf(delimiter, prefix.Length, StringComparison.OrdinalIgnoreCase);
            return indexOf < 0 ? key.Substring(prefix.Length) : key.Substring(prefix.Length, indexOf - prefix.Length);
        }
    }
}
