
using System.Collections.Generic;
using Microsoft.AspNet.ConfigurationModel.Sources;

namespace Microsoft.AspNet.ConfigurationModel
{
    public static class ConfigurationExtensions
    {
        public static IConfigurationSourceContainer AddIniFile(this IConfigurationSourceContainer configuration, string path)
        {
            configuration.Add(new IniFileConfigurationSource(path));
            return configuration;
        }

#if NET45
        public static IConfigurationSourceContainer AddCommandLine(this IConfigurationSourceContainer configuration)
        {
            configuration.Add(new CommandLineConfigurationSource());
            return configuration;
        }
#endif

        public static IConfigurationSourceContainer AddCommandLine(this IConfigurationSourceContainer configuration, string[] args)
        {
            configuration.Add(new CommandLineConfigurationSource(args));
            return configuration;
        }

        public static IConfigurationSourceContainer AddEnvironmentVariables(this IConfigurationSourceContainer configuration)
        {
            configuration.Add(new EnvironmentVariablesConfigurationSource());
            return configuration;
        }

        public static IConfigurationSourceContainer AddEnvironmentVariables(this IConfigurationSourceContainer configuration, string prefix)
        {
            configuration.Add(new EnvironmentVariablesConfigurationSource(prefix));
            return configuration;
        }
    }
}
