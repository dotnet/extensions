using System.Collections.Generic;

namespace Microsoft.Framework.Logging
{
#if ASPNET50 || ASPNETCORE50
    [Runtime.AssemblyNeutral]
#endif
    public interface ILoggerStructure
    {
        /// <summary>
        /// A brief message to give context for the structure being logged
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Returns an enumerable of key value pairs mapping the name of the structured data to the data.
        /// </summary>
        IEnumerable<KeyValuePair<string, object>> GetValues();

        /// <summary>
        /// Returns a human-readable string of the structured data
        /// </summary>
        string Format();
    }
}