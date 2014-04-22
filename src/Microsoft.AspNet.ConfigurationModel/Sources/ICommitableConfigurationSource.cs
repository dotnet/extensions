using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    [NotAssemblyNeutral]
    public interface ICommitableConfigurationSource
    {
        void Commit();
    }
}