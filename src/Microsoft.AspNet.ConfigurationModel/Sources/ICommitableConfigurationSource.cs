using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    [AssemblyNeutral]
    public interface ICommitableConfigurationSource : ISettableConfigurationSource
    {
        void Commit();
    }
}