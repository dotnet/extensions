using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    [NotAssemblyNeutral]
    public interface IExtendableConfiguration
    {
        void Add(IConfigurationSource configurationSource);
    }
}