using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.ConfigurationModel.Sources;

namespace Microsoft.AspNet.ConfigurationModel
{
    public class Configuration : IConfiguration, IConfigurationSourceContainer
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

            string value;
            return TryGet(key, out value) ? value : null;
        }

        public bool TryGet(string key, out string value)
        {
            if (key == null) throw new ArgumentNullException("key");

            for (int i = 0; i < _readableSources.Count; i++)
            {
                if (_readableSources[i].TryGet(key, out value))
                {
                    return true;
                }
            }
            value = null;
            return false;
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
            return new ConfigurationFocus(this, key + Constants.KeyDelimiter);
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

            return distinctSegments.Select(segment => CreateConfigurationFocus(prefix, segment));
        }

        private KeyValuePair<string, IConfiguration> CreateConfigurationFocus(string prefix, string segment)
        {
            return new KeyValuePair<string, IConfiguration>(
                segment,
                new ConfigurationFocus(this, prefix + segment + Constants.KeyDelimiter));
        }

        public IConfigurationSourceContainer Add(IConfigurationSource configurationSource)
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

        public IEnumerator<IConfigurationSource> GetEnumerator()
        {
            return _readableSources.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
