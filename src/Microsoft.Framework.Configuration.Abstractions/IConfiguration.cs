// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Framework.Configuration
{
    public interface IConfiguration
    {
        string this[string key] { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key">A case insensitive name.</param>
        /// <returns>The value associated with the given key, or null if none is found.</returns>
        string Get(string key);

        bool TryGet(string key, out string value);

        IConfiguration GetConfigurationSection(string key);

        IEnumerable<KeyValuePair<string, IConfiguration>> GetConfigurationSections();

        IEnumerable<KeyValuePair<string, IConfiguration>> GetConfigurationSections(string key);

        void Set(string key, string value);

        void Reload();
    }
}
