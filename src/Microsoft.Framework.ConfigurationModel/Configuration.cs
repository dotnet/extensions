// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.ConfigurationModel.Internal;

namespace Microsoft.Framework.ConfigurationModel
{
    public class Configuration : IConfiguration, IConfigurationSourceRoot
    {
        private readonly IList<IConfigurationSource> _sources = new List<IConfigurationSource>();

        public Configuration(params IConfigurationSource[] sources)
            : this(null, sources)
        {
        }

        public Configuration(string basePath, params IConfigurationSource[] sources)
        {
            if (sources != null)
            {
                foreach (var singleSource in sources)
                {
                    Add(singleSource);
                }
            }

            BasePath = basePath;
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

        public string BasePath
        {
            get;
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


        public void Set(string key, string value)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

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
            var segments = _sources.Aggregate(
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

        public IConfigurationSourceRoot Add(IConfigurationSource configurationSource)
        {
            configurationSource.Load();
            return AddLoadedSource(configurationSource);
        }

        internal IConfigurationSourceRoot AddLoadedSource(IConfigurationSource configurationSource)
        {
            _sources.Add(configurationSource);
            return this;
        }
    }
}
