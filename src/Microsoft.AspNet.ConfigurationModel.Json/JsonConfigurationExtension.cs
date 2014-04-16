using Microsoft.AspNet.ConfigurationModel.Sources;

namespace Microsoft.AspNet.ConfigurationModel
{
    public static class JsonConfigurationExtension
    {
        public static IConfigurationSourceContainer AddJsonFile(this IConfigurationSourceContainer configuration, string path)
        {
            configuration.Add(new JsonConfigurationSource(path));
            return configuration;
        }
    }
}
