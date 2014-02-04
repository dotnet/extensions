
namespace Microsoft.AspNet.Configuration
{
    public static class ConfigurationExtensions
    {
        public static ConfigurationContainer AddIniFile(this ConfigurationContainer container, string path)
        {
            return container.Add(new IniConfigurationFile(path));
        }
#if NET45
        public static ConfigurationContainer AddCommandLine(this ConfigurationContainer container)
        {
            return container.Add(new CommandLineConfiguration());
        }
#endif
        public static ConfigurationContainer AddEnvironment(this ConfigurationContainer container)
        {
            return container.Add(new EnvironmentConfiguration());
        }
    }
}
