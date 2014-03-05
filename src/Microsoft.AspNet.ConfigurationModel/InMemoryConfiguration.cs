using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.ConfigurationModel
{
    public class InMemoryConfiguration : BaseConfigurationSource, ISettableConfigurationSource
    {
        public InMemoryConfiguration()
        {
        }

        public InMemoryConfiguration(IEnumerable<KeyValuePair<string, string>> initialData)
        {
            foreach (var pair in initialData)
            {
                Data.Add(pair.Key, pair.Value);
            }
        }
    }
}
