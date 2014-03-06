using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    [NotAssemblyNeutral]
    public interface ICommitableConfigurationSource : ISettableConfigurationSource
    {
        void Commit();
    }
}