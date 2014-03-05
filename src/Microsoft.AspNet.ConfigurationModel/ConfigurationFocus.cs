using System.Collections.Generic;

namespace Microsoft.AspNet.ConfigurationModel
{
    public class ConfigurationFocus : IConfiguration
    {
        private readonly string _prefix;
        private readonly Configuration _root;

        public ConfigurationFocus(Configuration root, string prefix)
        {
            _prefix = prefix;
            _root = root;
        }

        public string Get(string key)
        {
            return _root.Get(_prefix + key);
        }

        public void Set(string key, string value)
        {
            _root.Set(_prefix + key, value);
        }

        public void Reload()
        {
            _root.Reload();
        }

        public void Commit()
        {
            _root.Commit();
        }

        public IEnumerable<KeyValuePair<string, IConfiguration>> Enumerate()
        {
            return _root.Enumerate(_prefix.Substring(_prefix.Length - 1));
        }

        public IEnumerable<KeyValuePair<string, IConfiguration>> Enumerate(string key)
        {
            return _root.Enumerate(_prefix + key);
        }
    }
}