// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using Microsoft.Framework.Cache.Distributed;
using Xunit;

namespace Microsoft.Framework.Cache.Redis
{
    public class RedisCacheSetAndRemoveTests
    {
        [Fact]
        public void GetMissingKeyReturnsFalseOrNull()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var value = new byte[1];
            string key = "non-existent-key";

            var result = cache.Get(key);
            Assert.Null(result);

            var found = cache.TryGetValue(key, out result);
            Assert.False(found);
        }

        [Fact]
        public void SetAndGetReturnsObject()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var value = new byte[1];
            string key = "myKey";

            var result = cache.Set(key, value);
            Assert.Equal(value, result);

            result = cache.Get(key);
            Assert.Equal(value, result);
        }

        [Fact]
        public void SetAndGetWorksWithCaseSensitiveKeys()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var value = new byte[1];
            string key1 = "myKey";
            string key2 = "Mykey";

            var result = cache.Set(key1, value);
            Assert.Equal(value, result);

            result = cache.Get(key1);
            Assert.Equal(value, result);

            result = cache.Get(key2);
            Assert.Null(result);
        }

        [Fact]
        public void GetOrSetDoesNotOverwrite()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var value1 = new byte[1] { 1 };
            var value2 = new byte[1] { 2 };
            string key = "myKey";

            // Assigned
            var result = cache.Set(key, context => value1);
            Assert.Equal(value1, result);

            // Retrieved
            result = cache.GetOrSet(key, context => value2);
            Assert.Equal(value1, result);
        }

        [Fact]
        public void SetAlwaysOverwrites()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var value1 = new byte[1] { 1 };
            string key = "myKey";

            var result = cache.Set(key, value1);
            Assert.Equal(value1, result);

            var value2 = new byte[1] { 2 };
            result = cache.Set(key, value2);
            Assert.Equal(value2, result);

            result = cache.Get(key);
            Assert.Equal(value2, result);
        }

        [Fact]
        public void RemoveRemoves()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var value = new byte[1];
            string key = "myKey";

            var result = cache.Set(key, value);
            Assert.Equal(value, result);

            cache.Remove(key);
            result = cache.Get(key);
            Assert.Null(result);
        }
    }
}