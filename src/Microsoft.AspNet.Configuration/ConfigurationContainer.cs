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

        public ConfigurationContainer AddFile(string path)
        {
            return Add(new FileConfiguration(path));
        }
#if NET45
        public ConfigurationContainer AddCommandLine()
        {
            return Add(new CommandLineConfiguration());
        }
#endif
        public ConfigurationContainer AddEnvironment()
        {
            return Add(new EnvironmentConfiguration());
        }
    }
}
