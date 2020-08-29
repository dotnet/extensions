// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Microsoft.Extensions.Caching.StackExchangeRedis
{
    /// <summary>
    /// Configuration options for <see cref="RedisCache"/>.
    /// </summary>
    public class RedisCacheOptions : IOptions<RedisCacheOptions>
    {
        /// <summary>
        /// The configuration used to connect to Redis.
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// The configuration used to connect to Redis.
        /// This is preferred over Configuration.
        /// </summary>
        public ConfigurationOptions ConfigurationOptions { get; set; }

        /// <summary>
        /// Use with a custom Redis ConnectionMultiplexer pool.  This is called for each async operation.
        /// </summary>
        public Func<CancellationToken, Task<ConnectionMultiplexer>> GetConnectionMultiplexerAsync { get; }

        /// <summary>
        /// Use with a custom Redis ConnectionMultiplexer pool.  This is called for each sync operation.
        /// </summary>
        public Func<ConnectionMultiplexer> GetConnectionMultiplexer { get; }

        /// <summary>
        /// The Redis instance name.
        /// </summary>
        public string InstanceName { get; set; }

        RedisCacheOptions IOptions<RedisCacheOptions>.Value
        {
            get { return this; }
        }
    }
}
