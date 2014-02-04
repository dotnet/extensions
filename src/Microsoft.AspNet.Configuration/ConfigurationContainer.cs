using System.Collections.Generic;

namespace Microsoft.AspNet.Configuration
{
    public class ConfigurationContainer : IConfiguration
    {
        private IList<IConfiguration> _configurations = new List<IConfiguration>();

        public ConfigurationContainer()
        {
        }

        public string Get(string key)
        {
            for (int i = 0; i < _configurations.Count; i++)
            {
                string value = _configurations[i].Get(key);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            return null;
        }

        public ConfigurationContainer Add(IConfiguration configuration)
        {
            _configurations.Add(configuration);
            return this;
        }
    }
}
