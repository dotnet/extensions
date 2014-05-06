// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.ConfigurationModel.Sources;

namespace Microsoft.Framework.ConfigurationModel
{
    public class Configuration : IConfiguration, IConfigurationSourceContainer
    {
        private readonly IList<IConfigurationSource> _sources = new List<IConfigurationSource>();
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

        public void Commit()
        {
            var final = _committableSources.LastOrDefault();
            if (final == null)
            {
                throw new Exception(Resources.Error_NoCommitableSource);
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

        public IConfigurationSourceContainer Add(IConfigurationSource configurationSource)
        {
            configurationSource.Load();
            return AddLoadedSource(configurationSource);
        }

        internal IConfigurationSourceContainer AddLoadedSource(IConfigurationSource configurationSource)
        {
            _sources.Add(configurationSource);

            if (configurationSource is ICommitableConfigurationSource)
            {
                _committableSources.Add(configurationSource as ICommitableConfigurationSource);
            }
            return this;
        }

        public IEnumerator<IConfigurationSource> GetEnumerator()
        {
            return _sources.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
