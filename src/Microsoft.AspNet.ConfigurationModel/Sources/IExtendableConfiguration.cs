using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    [AssemblyNeutral]
    public interface IExtendableConfiguration
    {
        void Add(IConfigurationSource configurationSource);
    }
}