// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using StackExchange.Redis;

namespace Microsoft.Framework.Cache.Redis
{
    internal static class RedisExtensions
    {
        private const string HmGetScript = (@"return redis.call('HMGET', KEYS[1], unpack(ARGV))");

        internal static RedisValue[] HashMemberGet(this IDatabase cache, string key, params string[] members)
        {
            var redisMembers = new RedisValue[members.Length];
            for (int i = 0; i < members.Length; i++)
            {
                redisMembers[i] = (RedisValue)members[i];
            }
            var result = cache.ScriptEvaluate(HmGetScript, new RedisKey[] { key }, redisMembers);
            // TODO: Error checking?
            return (RedisValue[])result;
        }
    }
}