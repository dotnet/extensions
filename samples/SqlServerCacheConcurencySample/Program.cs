// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Framework.Caching.Distributed;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Caching.SqlServer;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.Runtime;

namespace SqlServerCacheConcurrencySample
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
        private const string Key = "MyKey";
        private static readonly Random Random = new Random();
        private DistributedCacheEntryOptions _cacheEntryOptions;

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
            _cacheEntryOptions = new DistributedCacheEntryOptions();
            _cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromSeconds(10));

            var cache = new SqlServerCache(
                new CacheOptions(
                new SqlServerCacheOptions()
                {
                    ConnectionString = Configuration.Get("ConnectionString"),
                    SchemaName = Configuration.Get("SchemaName"),
                    TableName = Configuration.Get("TableName")
                }));
            cache.Connect();

            SetKey(cache, "0");

            PriodicallyReadKey(cache, TimeSpan.FromSeconds(1));

            PeriodciallyRemoveKey(cache, TimeSpan.FromSeconds(11));

            PeriodciallySetKey(cache, TimeSpan.FromSeconds(13));

            Console.ReadLine();
            Console.WriteLine("Shutting down");
        }

        private void SetKey(IDistributedCache cache, string value)
        {
            Console.WriteLine("Setting: " + value);
            cache.Set(Key, Encoding.UTF8.GetBytes(value), _cacheEntryOptions);
        }

        private void PeriodciallySetKey(IDistributedCache cache, TimeSpan interval)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(interval);

                    SetKey(cache, "A");
                }
            });
        }

        private void PriodicallyReadKey(IDistributedCache cache, TimeSpan interval)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(interval);

                    if (Random.Next(3) == 0) // 1/3 chance
                    {
                        // Allow values to expire due to sliding refresh.
                        Console.WriteLine("Read skipped, random choice.");
                    }
                    else
                    {
                        Console.Write("Reading...");
                        object result = cache.Get(Key);
                        if (result != null)
                        {
                            cache.Set(Key, Encoding.UTF8.GetBytes("B"), _cacheEntryOptions);
                        }
                        Console.WriteLine("Read: " + (result ?? "(null)"));
                    }
                }
            });
        }

        private void PeriodciallyRemoveKey(IDistributedCache cache, TimeSpan interval)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(interval);

                    Console.WriteLine("Removing...");
                    cache.Remove(Key);
                }
            });
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
