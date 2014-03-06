using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.ConfigurationModel.Sources;

namespace Microsoft.AspNet.ConfigurationModel
{
    public class Configuration : IConfiguration, IExtendableConfiguration
    {
        private readonly IList<IConfigurationSource> _readableSources = new List<IConfigurationSource>();
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
                string value;
                if (_readableSources[i].TryGet(key, out value))
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

        public IConfiguration GetSubKey(string key)
        {
            return new ConfigurationFocus(this, key);
        }

        public IEnumerable<KeyValuePair<string, IConfiguration>> GetSubKeys()
        {
            return GetSubKeysImplementation(string.Empty);
        }

        public IEnumerable<KeyValuePair<string, IConfiguration>> GetSubKeys(string key)
        {
            if (key == null) throw new ArgumentNullException("key");

            return GetSubKeysImplementation(key + Constants.KeyDelimiter);
        }

        private IEnumerable<KeyValuePair<string, IConfiguration>> GetSubKeysImplementation(string prefix)
        {
            var sources = _readableSources;
            
            var segments = sources.Aggregate(
                Enumerable.Empty<string>(),
                (seed, source) => source.ProduceSubKeys(seed, prefix, Constants.KeyDelimiter));

            var distinctSegments = segments.Distinct();

            return distinctSegments.Select(CreateConfigurationFocus);
        }

        private KeyValuePair<string, IConfiguration> CreateConfigurationFocus(string segment)
        {
            return new KeyValuePair<string, IConfiguration>(
                segment + Constants.KeyDelimiter, 
                new ConfigurationFocus(this, segment));
        }

        public void Add(IConfigurationSource configurationSource)
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
        }
    }
}
