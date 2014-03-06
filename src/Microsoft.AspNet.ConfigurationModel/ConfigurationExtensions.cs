
using Microsoft.AspNet.ConfigurationModel.Sources;

namespace Microsoft.AspNet.ConfigurationModel
{
    public static class ConfigurationExtensions
    {
        public static IExtendableConfiguration AddIniFile(this IExtendableConfiguration configuration, string path)
        {
            configuration.Add(new IniFileConfigurationSource(path));
            return configuration;
        }

#if NET45
        public static IExtendableConfiguration AddCommandLine(this IExtendableConfiguration configuration)
        {
            configuration.Add(new CommandLineConfigurationSource());
            return configuration;
        }
#endif

        public static IExtendableConfiguration AddEnvironmentVariables(this IExtendableConfiguration configuration)
        {
            configuration.Add(new EnvironmentVariablesConfigurationSource());
            return configuration;
        }

        public static IExtendableConfiguration AddEnvironmentVariables(this IExtendableConfiguration configuration, string prefix)
        {
            configuration.Add(new EnvironmentVariablesConfigurationSource(prefix));
            return configuration;
        }
    }
}
