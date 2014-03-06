using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    [AssemblyNeutral]
    public interface ISettableConfigurationSource : IConfigurationSource
    {
        void Set(string key, string value);
    }
}