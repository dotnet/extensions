// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.Configuration
{
    public class ConfigurationSection : ConfigurationBase, IConfigurationSection
    {
        private readonly string _key;

        public ConfigurationSection([NotNull] IList<IConfigurationSource> sources, [NotNull] string key)
            : base (sources)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException(Resources.Error_EmptyKey);
            }

            _key = key;
        }

        public string Key
        {
            get
            {
                return _key;
            }
        }

        public string Value
        {
            get
            {
                foreach (var src in Sources.Reverse())
                {
                    string value = null;

                    if (src.TryGet(_key, out value))
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
                    src.Set(Key, value);
                }
            }
        }

        protected override string GetPrefix()
        {
            return _key + Constants.KeyDelimiter;
        }
    }
}
