
namespace Microsoft.AspNet.Configuration
{
    public interface IConfiguration
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key">A case insensitive name.</param>
        /// <returns>The value associated with the given key, or null if none is found.</returns>
        string Get(string key);
    }
}
