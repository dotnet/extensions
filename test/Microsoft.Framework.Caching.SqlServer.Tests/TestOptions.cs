using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.Caching.SqlServer
{
    internal class TestSqlServerCacheOptions : IOptions<SqlServerCacheOptions>
    {
        private readonly SqlServerCacheOptions _innerOptions;

        public TestSqlServerCacheOptions(SqlServerCacheOptions innerOptions)
        {
            _innerOptions = innerOptions;
        }

        public SqlServerCacheOptions Options
        {
            get
            {
                return _innerOptions;
            }
        }

        public SqlServerCacheOptions GetNamedOptions(string name)
        {
            return _innerOptions;
        }
    }
}
