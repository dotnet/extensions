
namespace Microsoft.AspNet.ConfigurationModel
{
    public static class ConfigurationExtensions
    {
        public static Configuration AddIniFile(this Configuration container, string path)
        {
            return container.Add(new IniConfigurationFile(path));
        }

#if NET45
        public static Configuration AddCommandLine(this Configuration container)
        {
            return container.Add(new CommandLineConfiguration());
        }
#endif

        public static Configuration AddEnvironment(this Configuration container)
        {
            return container.Add(new EnvironmentConfiguration());
        }

        public static Configuration AddEnvironment(this Configuration container, string prefix)
        {
            return container.Add(new EnvironmentConfiguration(prefix));
        }
    }
}
