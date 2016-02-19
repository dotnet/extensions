// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    public class ConfigurationSection : IConfigurationSection
    {
        private readonly ConfigurationRoot _root;
        private readonly string _path;
        private string _key;

        public ConfigurationSection(ConfigurationRoot root, string path)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            _root = root;
            _path = path;
        }

        public string Path => _path;

        public string Key
        {
            get
            {
                if (_key == null)
                {
                    // Key is calculated lazily as last portion of Path
                    _key = ConfigurationPath.GetSectionKey(_path);
                }
                return _key;
            }
        }

        public string Value
        {
            get
            {
                return _root[Path];
            }
            set
            {
                _root[Path] = value;
            }
        }

        public string this[string key]
        {
            get
            {
                return _root[ConfigurationPath.Combine(Path, key)];
            }

            set
            {
                _root[ConfigurationPath.Combine(Path, key)] = value;
            }
        }

        public IConfigurationSection GetSection(string key) => _root.GetSection(ConfigurationPath.Combine(Path, key));

        public IEnumerable<IConfigurationSection> GetChildren() => _root.GetChildrenImplementation(Path);

        public IChangeToken GetReloadToken() => _root.GetReloadToken();
    }
}
