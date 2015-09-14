// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Framework.Configuration
{
    public abstract class ConfigurationBase : IConfiguration
    {
        private readonly IList<IConfigurationSource> _sources = new List<IConfigurationSource>();

        public ConfigurationBase(IList<IConfigurationSource> sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            _sources = sources;
        }

        public abstract string Path { get; }

        public string this[string key]
        {
            get
            {
                return GetSection(key).Value;
            }
            set
            {
                GetSection(key).Value = value;
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
            var segments = Sources.Aggregate(
                Enumerable.Empty<string>(),
                (seed, source) => source.ProduceConfigurationSections(seed, Path, Constants.KeyDelimiter));

            var distinctSegments = segments.Distinct();
            return distinctSegments.Select(segment =>
            {
                return new ConfigurationSection(Sources, Path, segment);
            });
        }

        public IConfigurationSection GetSection(string key)
        {
            return new ConfigurationSection(Sources, Path, key);
        }
    }
}
