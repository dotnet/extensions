using System;
using System.Collections;
using System.Linq;

namespace Microsoft.AspNet.ConfigurationModel
{
    public class EnvironmentConfiguration : BaseConfigurationSource
    {
        private readonly string _prefix;

        public EnvironmentConfiguration(string prefix)
        {
            _prefix = prefix;
        }

        public EnvironmentConfiguration()
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
