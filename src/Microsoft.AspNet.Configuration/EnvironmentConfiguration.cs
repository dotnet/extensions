using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Configuration
{
    public class EnvironmentConfiguration : IConfiguration
    {
        public EnvironmentConfiguration()
        {
        }

        public string Get(string key)
        {
            // Not case sensitive
            return Environment.GetEnvironmentVariable(key);
        }
    }
}
