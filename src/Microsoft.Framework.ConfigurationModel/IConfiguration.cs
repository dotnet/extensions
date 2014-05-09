// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Net.Runtime;

namespace Microsoft.Framework.ConfigurationModel
{
    //[AssemblyNeutral]
    public interface IConfiguration
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key">A case insensitive name.</param>
        /// <returns>The value associated with the given key, or null if none is found.</returns>
        string Get(string key);

        bool TryGet(string key, out string value);

        IConfiguration GetSubKey(string key);

        IEnumerable<KeyValuePair<string, IConfiguration>> GetSubKeys();

        IEnumerable<KeyValuePair<string, IConfiguration>> GetSubKeys(string key);

        void Reload();

        void Set(string key, string value);

        void Commit();
    }
}
