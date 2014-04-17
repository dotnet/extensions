using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    public class CommandLineConfigurationSource : BaseConfigurationSource
    {
#if NET45
        public CommandLineConfigurationSource()
            : this(Environment.GetCommandLineArgs())
        {
            Args = Environment.GetCommandLineArgs();
        }
#endif

        public CommandLineConfigurationSource(string[] args)
        {
            Args = args;
        }

        public string[] Args { get; private set; }

        public override void Load()
        {
#warning TODO - this is a placeholder algorithm which must be replaced

            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string pair in Args)
            {
                int split = pair.IndexOf('=');
                if (split <= 0)
                {
                    // TODO: exception message localization
                    throw new FormatException(string.Format("Unrecognized argument '{0}'.", pair));
                }

                string key = pair.Substring(0, split);
                string value = pair.Substring(split + 1);
                if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
                {
                    // Remove quotes
                    value = value.Substring(1, value.Length - 2);
                }

                if (data.ContainsKey(key))
                {
                    throw new FormatException(string.Format("Key '{0}' is duplicated.", key));
                }

                data[key] = value;
            }

            ReplaceData(data);
        }
    }
}
