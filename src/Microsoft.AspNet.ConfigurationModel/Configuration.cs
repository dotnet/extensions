using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.ConfigurationModel
{
    public class Configuration : IConfiguration
    {
        private readonly IList<IReadableConfigurationSource> _readableSources = new List<IReadableConfigurationSource>();
        private readonly IList<ISettableConfigurationSource> _settableSources = new List<ISettableConfigurationSource>();
        private readonly IList<ICommitableConfigurationSource> _committableSources = new List<ICommitableConfigurationSource>();

        public Configuration()
        {
        }

        public string Get(string key)
        {
            if (key == null) throw new ArgumentNullException("key");

            for (int i = 0; i < _readableSources.Count; i++)
            {
                string value = _readableSources[i].Get(key);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            return null;
        }

        public void Set(string key, string value)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            for (int i = 0; i < _settableSources.Count; i++)
            {
                _settableSources[i].Set(key, value);
            }
        }

        public void Reload()
        {
            for (int i = 0; i < _readableSources.Count; i++)
            {
                _readableSources[i].Load();
            }
        }

        public void Commit()
        {
            var final = _committableSources.LastOrDefault();
            if (final == null)
            {
                throw new Exception("TODO: no configuration sources capable of committing changes");
            }
            final.Commit();
        }

        public IEnumerable<KeyValuePair<string, IConfiguration>> Enumerate()
        {
            return _readableSources
                .SelectMany(source => source.EnumerateDistinct(string.Empty, Constants.KeyDelimiter))
                .Distinct()
                .Select(segment => new KeyValuePair<string, IConfiguration>(segment + Constants.KeyDelimiter, new ConfigurationFocus(this, segment)));
        }

        public IEnumerable<KeyValuePair<string, IConfiguration>> Enumerate(string key)
        {
            if (key == null) throw new ArgumentNullException("key");

            var prefix = key + Constants.KeyDelimiter;
            return _readableSources
                .SelectMany(source => source.EnumerateDistinct(prefix, Constants.KeyDelimiter))
                .Distinct()
                .Select(segment => new KeyValuePair<string, IConfiguration>(prefix + segment + Constants.KeyDelimiter, new ConfigurationFocus(this, segment)));
        }

        public Configuration Add(IReadableConfigurationSource configurationSource)
        {
            configurationSource.Load();

            _readableSources.Add(configurationSource);
            if (configurationSource is ISettableConfigurationSource)
            {
                _settableSources.Add(configurationSource as ISettableConfigurationSource);
            }
            if (configurationSource is ICommitableConfigurationSource)
            {
                _committableSources.Add(configurationSource as ICommitableConfigurationSource);
            }

            return this;
        }
    }
}
