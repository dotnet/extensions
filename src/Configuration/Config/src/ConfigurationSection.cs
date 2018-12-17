// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Represents a section of application configuration values.
    /// </summary>
    public class ConfigurationSection : IConfigurationSection
    {
        private readonly IConfigurationRoot _root;
        private string _key;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="root">The configuration root.</param>
        /// <param name="path">The path to this section.</param>
        public ConfigurationSection(IConfigurationRoot root, string path)
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
            Path = path;
        }

        /// <summary>
        /// Gets the full path to this section from the <see cref="IConfigurationRoot"/>.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the key this section occupies in its parent.
        /// </summary>
        public string Key => _key ?? (_key = ConfigurationPath.GetSectionKey(Path));

        /// <summary>
        /// Gets or sets the section value.
        /// </summary>
        public string Value
        {
            get => _root[Path];
            set => _root[Path] = value;
        }

        /// <summary>
        /// Gets or sets the value corresponding to a configuration key.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configuration value.</returns>
        public string this[string key]
        {
            get => _root[ConfigurationPath.Combine(Path, key)];
            set => _root[ConfigurationPath.Combine(Path, key)] = value;
        }

        /// <summary>
        /// Tries to get a configuration value for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>True</c> if a value for the specified key was found, otherwise <c>false</c>.</returns>
        public bool TryGet(string key, out string value)
            => _root.TryGet(ConfigurationPath.Combine(Path, key), out value);

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration section.</param>
        /// <returns>The <see cref="IConfigurationSection"/>.</returns>
        /// <remarks>
        ///     This method will never return <c>null</c>. If no matching sub-section is found with the specified key,
        ///     an empty <see cref="IConfigurationSection"/> will be returned.
        /// </remarks>
        public IConfigurationSection GetSection(string key) => _root.GetSection(ConfigurationPath.Combine(Path, key));

        /// <summary>
        /// Gets the immediate descendant configuration sub-sections.
        /// </summary>
        /// <returns>The configuration sub-sections.</returns>
        public IEnumerable<IConfigurationSection> GetChildren() => _root.GetChildrenImplementation(Path);

        /// <summary>
        /// Returns a <see cref="IChangeToken"/> that can be used to observe when this configuration is reloaded.
        /// </summary>
        /// <returns></returns>
        public IChangeToken GetReloadToken() => _root.GetReloadToken();
    }
}
