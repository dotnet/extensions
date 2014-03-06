using System.Collections.Generic;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    public class MemoryConfigurationSource : BaseConfigurationSource, ISettableConfigurationSource
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
    }
}
