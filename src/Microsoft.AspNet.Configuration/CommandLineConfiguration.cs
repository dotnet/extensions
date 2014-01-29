using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Configuration
{
    public class CommandLineConfiguration : IConfiguration
    {
        private IDictionary<string, string> _data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
#if NET45
        public CommandLineConfiguration()
            : this(Environment.GetCommandLineArgs())
        {
        }
#endif
        public CommandLineConfiguration(string[] args)
        {
            foreach (string pair in args)
            {
                int split = pair.IndexOf('=');
                if (split > 0)
                {
                    string key = pair.Substring(0, split);
                    string value = pair.Substring(split + 1);
                    if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
                    {
                        // Remove quotes
                        value = value.Substring(1, value.Length - 2);
                    }
                    _data[key] = value;
                }
            }
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
