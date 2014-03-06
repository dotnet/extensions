using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    [NotAssemblyNeutral]
    public interface ISettableConfigurationSource : IConfigurationSource
    {
        void Set(string key, string value);
    }
}