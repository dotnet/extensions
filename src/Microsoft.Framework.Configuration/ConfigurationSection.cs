// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Framework.Configuration
{
    public class ConfigurationSection : ConfigurationBase, IConfigurationSection
    {
        private readonly string _key;
        private readonly string _path;

        public ConfigurationSection(IList<IConfigurationProvider> providers, string parentPath, string key)
            : base(providers)
        {
            if (parentPath == null)
            {
                throw new ArgumentNullException(nameof(parentPath));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(Resources.Error_EmptyKey);
            }

            _key = key;
            if (!string.Equals(parentPath, string.Empty))
            {
                _path = parentPath + Constants.KeyDelimiter + key;
            }
            else
            {
                _path = key;
            }
        }

        public string Key
        {
            get
            {
                return _key;
            }
        }

        public override string Path
        {
            get
            {
                return _path;
            }
        }

        public string Value
        {
            get
            {
                foreach (var provider in Providers.Reverse())
                {
                    string value = null;

                    if (provider.TryGet(Path, out value))
                    {
                        return value;
                    }
                }

                return null;
            }
            set
            {
                if (!Providers.Any())
                {
                    throw new InvalidOperationException(Resources.Error_NoSources);
                }

                foreach (var provider in Providers)
                {
                    provider.Set(Path, value);
                }
            }
        }
    }
}
