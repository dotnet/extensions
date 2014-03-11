using System.Collections.Generic;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    [NotAssemblyNeutral]
    public interface IExtendableConfiguration : IConfiguration, IEnumerable<IConfigurationSource>
    {
        IExtendableConfiguration Add(IConfigurationSource configurationSource);
    }
}