// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Configuration.Internal;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.Configuration
{
    public class ConfigurationSection : IConfiguration
    {
        private readonly IList<IConfigurationSource> _sources = new List<IConfigurationSource>();

        public ConfigurationSection(IList<IConfigurationSource> sources)
        {
            _sources = sources;
        }

        public string this[string key]
        {
            get
            {
                return Get(key);
            }
            set
            {
                Set(key, value);
            }
        }

        public IEnumerable<IConfigurationSource> Sources
        {
            get
            {
                return _sources;
            }
        }

        public string Get([NotNull] string key)
        {
            string value;
            return TryGet(key, out value) ? value : null;
        }

        public bool TryGet([NotNull] string key, out string value)
        {
            // If a key in the newly added configuration source is identical to a key in a 
            // formerly added configuration source, the new one overrides the former one.
            // So we search in reverse order, starting with latest configuration source.
            foreach (var src in _sources.Reverse())
            {
                if (src.TryGet(key, out value))
                {
                    return true;
                }
            }
            value = null;
            return false;
        }

        public void Set([NotNull] string key, [NotNull] string value)
        {
            if (!_sources.Any())
            {
                throw new InvalidOperationException(Resources.Error_NoSources);
            }
            foreach (var src in _sources)
            {
                src.Set(key, value);
            }
        }

        public void Reload()
        {
            foreach (var src in _sources)
            {
                src.Load();
            }
        }

        public IConfiguration GetConfigurationSection(string key)
        {
            return new ConfigurationFocus(this, key + Constants.KeyDelimiter);
        }

        public IEnumerable<KeyValuePair<string, IConfiguration>> GetConfigurationSections()
        {
            return GetConfigurationSectionsImplementation(string.Empty);
        }

        public IEnumerable<KeyValuePair<string, IConfiguration>> GetConfigurationSections([NotNull] string key)
        {
            return GetConfigurationSectionsImplementation(key + Constants.KeyDelimiter);
        }

        private IEnumerable<KeyValuePair<string, IConfiguration>> GetConfigurationSectionsImplementation(string prefix)
        {
            var segments = _sources.Aggregate(
                Enumerable.Empty<string>(),
                (seed, source) => source.ProduceConfigurationSections(seed, prefix, Constants.KeyDelimiter));

            var distinctSegments = segments.Distinct();

            return distinctSegments.Select(segment => CreateConfigurationFocus(prefix, segment));
        }

        private KeyValuePair<string, IConfiguration> CreateConfigurationFocus(string prefix, string segment)
        {
            return new KeyValuePair<string, IConfiguration>(
                segment,
                new ConfigurationFocus(this, prefix + segment + Constants.KeyDelimiter));
        }
    }
}
