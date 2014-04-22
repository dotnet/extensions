using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    public class EnvironmentVariablesConfigurationSource : BaseConfigurationSource
    {
        private const string MySqlServerPrefix = "MYSQLCONNSTR_";
        private const string SqlAzureServerPrefix = "SQLAZURECONNSTR_";
        private const string SqlServerPrefix = "SQLCONNSTR_";
        private const string CustomPrefix = "CUSTOMCONNSTR_";
        private const string AppSettingPrefix = "APPSETTING_";

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

        public override void Load()
        {
            Load(Environment.GetEnvironmentVariables());
        }

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
            else if (key.StartsWith(AppSettingPrefix, StringComparison.OrdinalIgnoreCase))
            {
                yield return new DictionaryEntry(key.Substring(AppSettingPrefix.Length), entry.Value);
                yield break;
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
