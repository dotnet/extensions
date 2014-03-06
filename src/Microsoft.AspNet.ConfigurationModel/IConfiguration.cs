
using System.Collections.Generic;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.ConfigurationModel
{
    [NotAssemblyNeutral]
    public interface IConfiguration
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key">A case insensitive name.</param>
        /// <returns>The value associated with the given key, or null if none is found.</returns>
        string Get(string key);

        IConfiguration GetSubKey(string key);

        IEnumerable<KeyValuePair<string, IConfiguration>> GetSubKeys();

        IEnumerable<KeyValuePair<string, IConfiguration>> GetSubKeys(string key);

        void Reload();

        void Set(string key, string value);

        void Commit();
    }
}

namespace Microsoft.Net.Runtime
{
}
