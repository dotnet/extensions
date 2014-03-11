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

        public bool TryGet(string key, out string value)
        {
            return _root.TryGet(_prefix + key, out value);
        }

        public IConfiguration GetSubKey(string key)
        {
            return _root.GetSubKey(_prefix + key);
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

        public IEnumerable<KeyValuePair<string, IConfiguration>> GetSubKeys()
        {
            return _root.GetSubKeys(_prefix.Substring(0, _prefix.Length - 1));
        }

        public IEnumerable<KeyValuePair<string, IConfiguration>> GetSubKeys(string key)
        {
            return _root.GetSubKeys(_prefix + key);
        }
    }
}