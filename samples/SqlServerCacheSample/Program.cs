// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Framework.Caching.Distributed;
using Microsoft.Framework.Caching.SqlServer;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.Runtime;

namespace SqlServerCacheSample
{
    /// <summary>
    /// This sample requires setting up a Microsoft SQL Server based cache database.
    /// 1. Install the command globally by doing "dnx commands install Microsoft.Framework.Caching.SqlServer". This
    ///    installs a command called "sqlservercache".
    /// 2. Create a new database in the SQL Server or use as existing gone.
    /// 3. Run the command "sqlservercache create <connectionstring> <schemName> <tableName>" to setup the table and
    ///    indexes.
    /// 4. Run this sample by doing "dnx . run"
    /// </summary>
    public class Program
    {
        public Program(IApplicationEnvironment appEnv)
        {
            var configurationBuilder = new ConfigurationBuilder(appEnv.ApplicationBasePath);
            configurationBuilder.AddJsonFile("config.json")
                        .AddEnvironmentVariables();
            Configuration = configurationBuilder.Build();
        }

        public IConfiguration Configuration { get; }

        public void Main()
        {
            RunSampleAsync().Wait();
        }

        public async Task RunSampleAsync()
        {
            var key = Guid.NewGuid().ToString();
            var message = "Hello, World!";
            var value = Encoding.UTF8.GetBytes(message);

            Console.WriteLine("Connecting to cache");
            var cache = new SqlServerCache(
                new CacheOptions(
                new SqlServerCacheOptions()
                {
                    ConnectionString = Configuration.Get("ConnectionString"),
                    SchemaName = Configuration.Get("SchemaName"),
                    TableName = Configuration.Get("TableName")
                }));
            await cache.ConnectAsync();

            Console.WriteLine("Connected");

            Console.WriteLine("Cache item key: {0}", key);
            Console.WriteLine($"Setting value '{message}' in cache");
            await cache.SetAsync(
                key,
                value,
                new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(10)));
            Console.WriteLine("Set");

            Console.WriteLine("Getting value from cache");
            value = await cache.GetAsync(key);
            if (value != null)
            {
                Console.WriteLine("Retrieved: " + Encoding.UTF8.GetString(value));
            }
            else
            {
                Console.WriteLine("Not Found");
            }

            Console.WriteLine("Refreshing value in cache");
            await cache.RefreshAsync(key);
            Console.WriteLine("Refreshed");

            Console.WriteLine("Removing value from cache");
            await cache.RemoveAsync(key);
            Console.WriteLine("Removed");

            Console.WriteLine("Getting value from cache again");
            value = await cache.GetAsync(key);
            if (value != null)
            {
                Console.WriteLine("Retrieved: " + Encoding.UTF8.GetString(value));
            }
            else
            {
                Console.WriteLine("Not Found");
            }

            Console.ReadLine();
        }

        private class CacheOptions : IOptions<SqlServerCacheOptions>
        {
            private readonly SqlServerCacheOptions _innerOptions;

            public CacheOptions(SqlServerCacheOptions innerOptions)
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
}
