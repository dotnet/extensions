using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    public class MemoryConfigurationSource : 
        BaseConfigurationSource, 
        IEnumerable<KeyValuePair<string,string>>
    {
        public MemoryConfigurationSource()
        {
        }

        public MemoryConfigurationSource(IEnumerable<KeyValuePair<string, string>> initialData)
        {
            foreach (var pair in initialData)
            {
                Data.Add(pair.Key, pair.Value);
            }
        }

        public void Add(string key, string value)
        {
            Data.Add(key, value);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return Data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
