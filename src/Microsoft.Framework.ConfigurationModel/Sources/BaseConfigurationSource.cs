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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Framework.ConfigurationModel.Sources
{
    public abstract class BaseConfigurationSource : IConfigurationSource
    {
        protected BaseConfigurationSource()
        {
            Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public IDictionary<string, string> Data { get; private set; }

        protected virtual void ReplaceData(Dictionary<string, string> data)
        {
            Data = data;
        }

        public virtual bool TryGet(string key, out string value)
        {
            return Data.TryGetValue(key, out value);
        }

        public virtual void Set(string key, string value)
        {
            Data[key] = value;
        }

        public virtual void Load()
        {
        }
       
        public virtual IEnumerable<string> ProduceSubKeys(IEnumerable<string> earlierKeys, string prefix, string delimiter)
        {
            return Data
                .Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(kv => Segment(kv.Key, prefix, delimiter))
                .Concat(earlierKeys);
        }

        private static string Segment(string key, string prefix, string delimiter)
        {
            var indexOf = key.IndexOf(delimiter, prefix.Length, StringComparison.OrdinalIgnoreCase);
            return indexOf < 0 ? key.Substring(prefix.Length) : key.Substring(prefix.Length, indexOf - prefix.Length);
        }
    }
}
