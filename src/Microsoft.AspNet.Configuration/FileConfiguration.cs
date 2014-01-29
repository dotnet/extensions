using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNet.Configuration
{
    public class FileConfiguration : IConfiguration
    {
        private IDictionary<string, string> _data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public FileConfiguration(string path)
        {
            foreach (string line in File.ReadLines(path))
            {
                int split = line.IndexOf('=');
                if (split > 0)
                {
                    string key = line.Substring(0, split);
                    string value = line.Substring(split + 1);
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
