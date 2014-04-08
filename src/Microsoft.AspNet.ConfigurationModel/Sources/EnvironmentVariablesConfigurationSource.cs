using System;
using System.Collections;
using System.Linq;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    public class EnvironmentVariablesConfigurationSource : BaseConfigurationSource
    {
        private readonly string _prefix;
        private static readonly string AzureConnStrPrefix = "SQLAZURECONNSTR_";

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
            ReplaceData(Environment.GetEnvironmentVariables()
                .Cast<DictionaryEntry>()
                .Select(entry => 
                    ((string)entry.Key).StartsWith(AzureConnStrPrefix, StringComparison.OrdinalIgnoreCase) ? 
                    new DictionaryEntry(((string)entry.Key).Substring(AzureConnStrPrefix.Length), entry.Value) : entry)
                .Where(entry => ((string)entry.Key).StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    entry => ((string)entry.Key).Substring(_prefix.Length),
                    entry => (string)entry.Value,
                    StringComparer.OrdinalIgnoreCase));
        }
    }
}
