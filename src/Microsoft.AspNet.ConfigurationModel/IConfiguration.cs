
using System.Collections.Generic;

namespace Microsoft.AspNet.ConfigurationModel
{
    public interface IConfiguration
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key">A case insensitive name.</param>
        /// <returns>The value associated with the given key, or null if none is found.</returns>
        string Get(string key);

        void Set(string key, string value);

        void Reload();

        void Commit();

        IEnumerable<KeyValuePair<string, IConfiguration>> Enumerate();

        IEnumerable<KeyValuePair<string, IConfiguration>> Enumerate(string key);
    }

    public interface IReadableConfigurationSource
    {
        string Get(string key);

        void Load();

        IEnumerable<string> EnumerateDistinct(string prefix, string delimiter);
    }

    public interface ISettableConfigurationSource : IReadableConfigurationSource
    {
        void Set(string key, string value);
    }

    public interface ICommitableConfigurationSource : ISettableConfigurationSource
    {
        void Commit();
    }
}
