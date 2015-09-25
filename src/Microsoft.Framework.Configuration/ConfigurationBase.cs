// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Framework.Configuration
{
    public abstract class ConfigurationBase : IConfiguration
    {
        private readonly IList<IConfigurationProvider> _providers = new List<IConfigurationProvider>();

        public ConfigurationBase(IList<IConfigurationProvider> providers)
        {
            if (providers == null)
            {
                throw new ArgumentNullException(nameof(providers));
            }

            _providers = providers;
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

        public IList<IConfigurationProvider> Providers
        {
            get
            {
                return _providers;
            }
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return Providers
                .Aggregate(Enumerable.Empty<string>(),
                    (seed, source) => source.GetChildKeys(seed, Path, Constants.KeyDelimiter))
                .Distinct()
                .Select(GetSection);
        }

        public IConfigurationSection GetSection(string key)
        {
            return new ConfigurationSection(Providers, Path, key);
        }
    }
}
