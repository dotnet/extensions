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

using System.Collections.Generic;

namespace Msft.Framework.ConfigurationModel
{
    public class ConfigurationFocus : IConfiguration
    {
        private readonly string _prefix;
        private readonly Configuration _root;

        public ConfigurationFocus(Configuration root, string prefix)
        {
            _prefix = prefix;
            _root = root;
        }

        public string Get(string key)
        {
            // Null key indicates that the prefix passed to ctor should be used as a key
            if (key == null)
            {
                // Strip off the trailing colon to get a valid key
                var defaultKey = _prefix.Substring(0, _prefix.Length - 1);
                return _root.Get(defaultKey);
            }

            return _root.Get(_prefix + key);
        }

        public bool TryGet(string key, out string value)
        {
            // Null key indicates that the prefix passed to ctor should be used as a key
            if (key == null)
            {
                // Strip off the trailing colon to get a valid key
                var defaultKey = _prefix.Substring(0, _prefix.Length - 1);
                return _root.TryGet(defaultKey, out value);
            }
            return _root.TryGet(_prefix + key, out value);
        }

        public IConfiguration GetSubKey(string key)
        {
            return _root.GetSubKey(_prefix + key);
        }

        public void Set(string key, string value)
        {
            _root.Set(_prefix + key, value);
        }

        public void Reload()
        {
            _root.Reload();
        }

        public void Commit()
        {
            _root.Commit();
        }

        public IEnumerable<KeyValuePair<string, IConfiguration>> GetSubKeys()
        {
            return _root.GetSubKeys(_prefix.Substring(0, _prefix.Length - 1));
        }

        public IEnumerable<KeyValuePair<string, IConfiguration>> GetSubKeys(string key)
        {
            return _root.GetSubKeys(_prefix + key);
        }
    }
}