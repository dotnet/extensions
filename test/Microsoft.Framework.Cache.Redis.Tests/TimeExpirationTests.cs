// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading;
using Microsoft.AspNet.Testing;
using Microsoft.Framework.Cache.Distributed;
using Xunit;

namespace Microsoft.Framework.Cache.Redis
{
    public class TimeExpirationTests
    {
        [Fact]
        public void AbsoluteExpirationInThePastThrows()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            var expected = DateTimeOffset.Now - TimeSpan.FromMinutes(1);
            ExceptionAssert.ThrowsArgumentOutOfRange(() =>
            {
                var result = cache.Set(key, context =>
                {
                    context.SetAbsoluteExpiration(expected);
                    return value;
                });
            }, "absolute", "The absolute expiration value must be in the future.", expected.ToString(CultureInfo.CurrentCulture));
        }

        [Fact]
        public void AbsoluteExpirationExpires()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            var result = cache.Set(key, context =>
            {
                context.SetAbsoluteExpiration(DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1));
                return value;
            });
            Assert.Equal(value, result);

            var found = cache.TryGetValue(key, out result);
            Assert.True(found);
            Assert.Equal(value, result);

            for (int i = 0; i < 4 && found; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
                found = cache.TryGetValue(key, out result);
            }

            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void AbsoluteSubSecondExpirationExpiresImmidately()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            var result = cache.Set(key, context =>
            {
                context.SetAbsoluteExpiration(DateTimeOffset.UtcNow + TimeSpan.FromSeconds(0.25));
                return value;
            });
            Assert.Equal(value, result);

            var found = cache.TryGetValue(key, out result);
            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void NegativeRelativeExpirationThrows()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            ExceptionAssert.ThrowsArgumentOutOfRange(() =>
            {
                var result = cache.Set(key, context =>
                {
                    context.SetAbsoluteExpiration(TimeSpan.FromMinutes(-1));
                    return value;
                });
            }, "relative", "The relative expiration value must be positive.", TimeSpan.FromMinutes(-1));
        }

        [Fact]
        public void ZeroRelativeExpirationThrows()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            ExceptionAssert.ThrowsArgumentOutOfRange(() =>
            {
                var result = cache.Set(key, context =>
                {
                    context.SetAbsoluteExpiration(TimeSpan.Zero);
                    return value;
                });
            }, "relative", "The relative expiration value must be positive.", TimeSpan.Zero);
        }

        [Fact]
        public void RelativeExpirationExpires()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            var result = cache.Set(key, context =>
            {
                context.SetAbsoluteExpiration(TimeSpan.FromSeconds(1));
                return value;
            });
            Assert.Equal(value, result);

            var found = cache.TryGetValue(key, out result);
            Assert.True(found);
            Assert.Equal(value, result);
          
            for (int i = 0; i < 4 && found; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
                found = cache.TryGetValue(key, out result);
            }
            Assert.False(found);
        }

        [Fact]
        public void RelativeSubSecondExpirationExpiresImmediately()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            var result = cache.Set(key, context =>
            {
                context.SetAbsoluteExpiration(TimeSpan.FromSeconds(0.25));
                return value;
            });
            Assert.Equal(value, result);

            var found = cache.TryGetValue(key, out result);
            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void NegativeSlidingExpirationThrows()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            ExceptionAssert.ThrowsArgumentOutOfRange(() =>
            {
                var result = cache.Set(key, context =>
                {
                    context.SetSlidingExpiration(TimeSpan.FromMinutes(-1));
                    return value;
                });
            }, "offset", "The sliding expiration value must be positive.", TimeSpan.FromMinutes(-1));
        }

        [Fact]
        public void ZeroSlidingExpirationThrows()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            ExceptionAssert.ThrowsArgumentOutOfRange(() =>
            {
                var result = cache.Set(key, context =>
                {
                    context.SetSlidingExpiration(TimeSpan.Zero);
                    return value;
                });
            }, "offset", "The sliding expiration value must be positive.", TimeSpan.Zero);
        }

        [Fact]
        public void SlidingExpirationExpiresIfNotAccessed()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            var result = cache.Set(key, context =>
            {
                context.SetSlidingExpiration(TimeSpan.FromSeconds(1));
                return value;
            });
            Assert.Equal(value, result);

            var found = cache.TryGetValue(key, out result);
            Assert.True(found);
            Assert.Equal(value, result);

            Thread.Sleep(TimeSpan.FromSeconds(3));

            found = cache.TryGetValue(key, out result);
            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void SlidingSubSecondExpirationExpiresImmediately()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            var result = cache.Set(key, context =>
            {
                context.SetSlidingExpiration(TimeSpan.FromSeconds(0.25));
                return value;
            });
            Assert.Equal(value, result);

            var found = cache.TryGetValue(key, out result);
            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void SlidingExpirationRenewedByAccess()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            var result = cache.Set(key, context =>
            {
                context.SetSlidingExpiration(TimeSpan.FromSeconds(1));
                return value;
            });
            Assert.Equal(value, result);

            var found = cache.TryGetValue(key, out result);
            Assert.True(found);
            Assert.Equal(value, result);
            
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));

                found = cache.TryGetValue(key, out result);
                Assert.True(found);
                Assert.Equal(value, result);
            }

            Thread.Sleep(TimeSpan.FromSeconds(3));
            found = cache.TryGetValue(key, out result);

            Assert.False(found);
            Assert.Null(result);
        }

        [Fact]
        public void SlidingExpirationRenewedByAccessUntilAbsoluteExpiration()
        {
            var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
            var key = "myKey";
            var value = new byte[1];

            var result = cache.Set(key, context =>
            {
                context.SetSlidingExpiration(TimeSpan.FromSeconds(1));
                context.SetAbsoluteExpiration(TimeSpan.FromSeconds(3));
                return value;
            });
            Assert.Equal(value, result);

            var found = cache.TryGetValue(key, out result);
            Assert.True(found);
            Assert.Equal(value, result);

            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));

                found = cache.TryGetValue(key, out result);
                Assert.True(found);
                Assert.Equal(value, result);
            }

            Thread.Sleep(TimeSpan.FromSeconds(1));

            found = cache.TryGetValue(key, out result);
            Assert.False(found);
            Assert.Null(result);
        }
    }
}