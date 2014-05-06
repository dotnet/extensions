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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Framework.ConfigurationModel
{
    public class EnvironmentVariablesConfigurationSource : BaseConfigurationSource
    {
        private const string MySqlServerPrefix = "MYSQLCONNSTR_";
        private const string SqlAzureServerPrefix = "SQLAZURECONNSTR_";
        private const string SqlServerPrefix = "SQLCONNSTR_";
        private const string CustomPrefix = "CUSTOMCONNSTR_";

        private const string ConnStrKeyFormat = "Data:{0}:ConnectionString";
        private const string ProviderKeyFormat = "Data:{0}:ProviderName";

        private readonly string _prefix;

        public EnvironmentVariablesConfigurationSource(string prefix)
        {
            _prefix = prefix;
        }

        public EnvironmentVariablesConfigurationSource()
        {
            _prefix = string.Empty;
        }

#if NET45 || K10
        public override void Load()
        {
            Load(Environment.GetEnvironmentVariables());
        }
#endif

        internal void Load(IDictionary envVariables)
        {
            ReplaceData(envVariables
                .Cast<DictionaryEntry>()
                .SelectMany(AzureEnvToAppEnv)
                .Where(entry => ((string)entry.Key).StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    entry => ((string)entry.Key).Substring(_prefix.Length),
                    entry => (string)entry.Value,
                    StringComparer.OrdinalIgnoreCase));
        }

        private static IEnumerable<DictionaryEntry> AzureEnvToAppEnv(DictionaryEntry entry)
        {
            var key = (string)entry.Key;
            var prefix = string.Empty;
            var provider = string.Empty;

            if (key.StartsWith(MySqlServerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                prefix = MySqlServerPrefix;
                provider = "MySql.Data.MySqlClient";
            }
            else if (key.StartsWith(SqlAzureServerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                prefix = SqlAzureServerPrefix;
                provider = "System.Data.SqlClient";
            }
            else if (key.StartsWith(SqlServerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                prefix = SqlServerPrefix;
                provider = "System.Data.SqlClient";
            }
            else if (key.StartsWith(CustomPrefix, StringComparison.OrdinalIgnoreCase))
            {
                prefix = CustomPrefix;
            }
            else
            {
                yield return entry;
                yield break;
            }

            // Return the key-value pair for connection string
            yield return new DictionaryEntry(
                string.Format(ConnStrKeyFormat, key.Substring(prefix.Length)),
                entry.Value);

            if (!string.IsNullOrEmpty(provider))
            {
                // Return the key-value pair for provider name
                yield return new DictionaryEntry(
                    string.Format(ProviderKeyFormat, key.Substring(prefix.Length)),
                    provider);
            }
        }
    }
}
