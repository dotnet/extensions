using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Framework.Logging
{
    public abstract class LoggerStructureBase : ILoggerStructure
    {
        public string Message { get; set; }

        public virtual IEnumerable<KeyValuePair<string, object>> GetValues()
        {
            var values = new List<KeyValuePair<string, object>>();
            var properties = GetType().GetTypeInfo().DeclaredProperties;
            foreach (var propertyInfo in properties)
            {
                values.Add(new KeyValuePair<string, object>(
                    propertyInfo.Name,
                    propertyInfo.GetValue(this)));
            }
            return values;
        }

        public abstract string Format();
    }
}