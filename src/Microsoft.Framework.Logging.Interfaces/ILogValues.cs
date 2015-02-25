using System.Collections.Generic;

namespace Microsoft.Framework.Logging
{
    public interface ILogValues
    {
        /// <summary>
        /// Returns an enumerable of key value pairs mapping the name of the structured data to the data.
        /// </summary>
        IEnumerable<KeyValuePair<string, object>> GetValues();

        /// <summary>
        /// Returns a human-readable string of the structured data.
        /// </summary>
        string Format();
    }
}