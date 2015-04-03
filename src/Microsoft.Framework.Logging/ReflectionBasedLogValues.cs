using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Framework.Logging
{
    public class ReflectionBasedLogValues : ILogValues
    {
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
    }
}