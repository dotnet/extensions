// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Configuration.EnvironmentVariables
{
    /// <summary>
    /// An environment variable based <see cref="ConfigurationProvider"/>.
    /// </summary>
    public class EnvironmentVariablesConfigurationProvider : ConfigurationProvider
    {
        private const string ConnStrKeyFormat = "ConnectionStrings:{0}";
        private const string ProviderKeyFormat = "ConnectionStrings:{0}_ProviderName";

        private readonly string _prefix;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public EnvironmentVariablesConfigurationProvider() : this(string.Empty)
        { }

        /// <summary>
        /// Initializes a new instance with the specified prefix.
        /// </summary>
        /// <param name="prefix">A prefix used to filter the environment variables.</param>
        public EnvironmentVariablesConfigurationProvider(string prefix)
        {
            _prefix = prefix ?? string.Empty;
        }

        /// <summary>
        /// Loads the environment variables.
        /// </summary>
        public override void Load()
        {
            Load(Environment.GetEnvironmentVariables());
        }

        internal void Load(IDictionary envVariables)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var filteredEnvVariables = envVariables
                .Cast<DictionaryEntry>()
                .SelectMany(AzureEnvToAppEnv)
                .Where(entry => ((string)entry.Key).StartsWith(_prefix, StringComparison.OrdinalIgnoreCase));

            foreach (var envVariable in filteredEnvVariables)
            {
                var key = ((string)envVariable.Key).Substring(_prefix.Length);
                data[key] = (string)envVariable.Value;
            }

            Data = data;
        }

        private static string NormalizeKey(string key)
        {
            return key.Replace("__", ConfigurationPath.KeyDelimiter);
        }

        private static IEnumerable<DictionaryEntry> AzureEnvToAppEnv(DictionaryEntry entry)
        {
            var key = (string)entry.Key;
            var (prefix, provider) = GetPrefixAndProvider(key);

            if (string.IsNullOrEmpty(prefix))
            {
                entry.Key = NormalizeKey(key);
                yield return entry;
                yield break;
            }

            // Return the key-value pair for connection string
            yield return new DictionaryEntry(
                string.Format(ConnStrKeyFormat, NormalizeKey(key.Substring(prefix.Length))),
                entry.Value);

            if (!string.IsNullOrEmpty(provider))
            {
                // Return the key-value pair for provider name
                yield return new DictionaryEntry(
                    string.Format(ProviderKeyFormat, NormalizeKey(key.Substring(prefix.Length))),
                    provider);
            }
        }

        private static (string prefix, string provider) GetPrefixAndProvider(string key)
        {
            return new (string prefix, string provider)[]
                {
                    ("APIHUBCONNSTR_", string.Empty),
                    ("DOCDBCONNSTR_", string.Empty),
                    ("CUSTOMCONNSTR_", string.Empty),
                    ("EVENTHUBCONNSTR_", string.Empty),
                    ("NOTIFICATIONHUBCONNSTR_", string.Empty),
                    ("MYSQLCONNSTR_", "MySql.Data.MySqlClient"),
                    ("POSTGRESQLCONNSTR_", string.Empty),
                    ("REDISCACHECONNSTR_", string.Empty),
                    ("SERVICEBUSCONNSTR_", string.Empty),
                    ("SQLAZURECONNSTR_", "System.Data.SqlClient"),
                    ("SQLCONNSTR_", "System.Data.SqlClient")
                }
                .Where(t => key.StartsWith(t.prefix, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }
    }
}
