// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using StackExchange.Redis;

namespace Microsoft.Extensions.Caching.StackExchangeRedis
{
    internal static class RedisExtensions
    {
        internal static RedisValue[] HashMemberGet(this IDatabase cache, string key, params string[] members)
        {
            // TODO: Error checking?
            return cache.HashGet(key, GetRedisMembers(members));
        }

        internal static async Task<RedisValue[]> HashMemberGetAsync(
            this IDatabase cache,
            string key,
            params string[] members)
        {
            // TODO: Error checking?
            return await cache.HashGetAsync(key, GetRedisMembers(members)).ConfigureAwait(false);
        }

        private static RedisValue[] GetRedisMembers(params string[] members)
        {
            var redisMembers = new RedisValue[members.Length];
            for (int i = 0; i < members.Length; i++)
            {
                redisMembers[i] = (RedisValue)members[i];
            }

            return redisMembers;
        }
    }
}
