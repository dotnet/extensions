// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.Configuration
{
    public abstract class ConfigurationBase : IConfiguration
    {
        private readonly IList<IConfigurationSource> _sources = new List<IConfigurationSource>();

        public ConfigurationBase ([NotNull] IList<IConfigurationSource> sources)
        {
            _sources = sources;
        }

        public string this[[NotNull] string key]
        {
            get
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new InvalidOperationException(Resources.Error_EmptyKey);
                }

                // If a key in the newly added configuration source is identical to a key in a
                // formerly added configuration source, the new one overrides the former one.
                // So we search in reverse order, starting with latest configuration source.
                foreach (var src in _sources.Reverse())
                {
                    string value = null;

                    if (src.TryGet(GetPrefix() + key, out value))
                    {
                        return value;
                    }
                }

                return null;
            }
            set
            {
                if (!Sources.Any())
                {
                    throw new InvalidOperationException(Resources.Error_NoSources);
                }

                foreach (var src in Sources)
                {
                    src.Set(GetPrefix() + key, value);
                }
            }
        }

        public IList<IConfigurationSource> Sources
        {
            get
            {
                return _sources;
            }
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            var prefix = GetPrefix();

            var segments = Sources.Aggregate(
                Enumerable.Empty<string>(),
                (seed, source) => source.ProduceConfigurationSections(seed, prefix, Constants.KeyDelimiter));

            var distinctSegments = segments.Distinct();
            return distinctSegments.Select(segment =>
            {
                return new ConfigurationSection(Sources, prefix + segment);
            });
        }

        public IConfigurationSection GetSection(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException(Resources.Error_EmptyKey);
            }

            return new ConfigurationSection(Sources, GetPrefix() + key);
        }

        protected abstract string GetPrefix();
    }
}
