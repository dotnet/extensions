using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.ConfigurationModel
{
    public class CommandLineConfiguration : BaseConfigurationSource
    {
        public string[] Args { get; set; }

#if NET45
        public CommandLineConfiguration()
            : this(Environment.GetCommandLineArgs())
        {
            Args = Environment.GetCommandLineArgs();
        }
#endif

        public CommandLineConfiguration(string[] args)
        {
            Args = args;
        }

        public override void Load()
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string pair in Args)
            {
                int split = pair.IndexOf('=');
                if (split > 0)
                {
                    string key = pair.Substring(0, split);
                    string value = pair.Substring(split + 1);
                    if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
                    {
                        // Remove quotes
                        value = value.Substring(1, value.Length - 2);
                    }
                    data[key] = value;
                }
            }
            ReplaceData(data);
        }
    }
}
