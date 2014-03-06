using System;
using System.Collections;
using System.Linq;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    public class EnvironmentVariablesConfigurationSource : BaseConfigurationSource
    {
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
#if NET45
            ReplaceData(Environment.GetEnvironmentVariables()
                .Cast<DictionaryEntry>()
                .Where(entry => ((string) entry.Key).StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    entry => ((string) entry.Key).Substring(_prefix.Length),
                    entry => (string) entry.Value,
                    StringComparer.OrdinalIgnoreCase));
#endif
        }
    }
}
