using System.Collections.Generic;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    [AssemblyNeutral]
    public interface IConfigurationSource
    {
        bool TryGet(string key, out string value);

        void Load();

        IEnumerable<string> ProduceSubKeys(IEnumerable<string> earlierKeys, string prefix, string delimiter);
    }
}