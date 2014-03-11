using System.Collections.Generic;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    [NotAssemblyNeutral]
    public interface IConfigurationSourceContainer : IConfiguration, IEnumerable<IConfigurationSource>
    {
        IConfigurationSourceContainer Add(IConfigurationSource configurationSource);
    }
}