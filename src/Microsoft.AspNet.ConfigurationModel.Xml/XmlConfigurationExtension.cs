using Microsoft.AspNet.ConfigurationModel.Sources;

namespace Microsoft.AspNet.ConfigurationModel
{
    public static class XmlConfigurationExtension
    {
        public static IConfigurationSourceContainer AddXmlFile(this IConfigurationSourceContainer configuration, string path)
        {
            configuration.Add(new XmlConfigurationSource(path));
            return configuration;
        }
    }
}
