// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using Xunit;

namespace Microsoft.Framework.Cache.Memory
{
    public class MemoryCacheSetAndRemoveTests
    {
        private IMemoryCache CreateCache()
        {
            return new MemoryCache(new MemoryCacheOptions()
            {
                ListenForMemoryPressure = false,
            });
        }

        [Fact]
        public void GetMissingKeyReturnsFalseOrNull()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            var result = cache.Get(key);
            Assert.Null(result);

            var found = cache.TryGetValue(key, out result);
            Assert.False(found);
        }

        [Fact]
        public void SetAndGetReturnsObject()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            var result = cache.Set(key, obj);
            Assert.Same(obj, result);

            result = cache.Get(key);
            Assert.Same(obj, result);
        }

        [Fact]
        public void SetAndGetWorksWithCaseSensitiveKeys()
        {
            var cache = CreateCache();
            var obj = new object();
            string key1 = "myKey";
            string key2 = "Mykey";

            var result = cache.Set(key1, obj);
            Assert.Same(obj, result);

            result = cache.Get(key1);
            Assert.Same(obj, result);

            result = cache.Get(key2);
            Assert.Null(result);
        }

        [Fact]
        public void GetOrSetDoesNotOverwrite()
        {
            var cache = CreateCache();
            var obj = new object();
            var obj2 = new object();
            string key = "myKey";

            // Assigned
            var result = cache.GetOrSet(key, context => obj);
            Assert.Same(obj, result);

            // Retrieved
            result = cache.GetOrSet(key, context => obj2);
            Assert.Same(obj, result);
        }

        [Fact]
        public void SetAlwaysOverwrites()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            var result = cache.Set(key, obj);
            Assert.Same(obj, result);

            var obj2 = new object();
            result = cache.Set(key, obj2);
            Assert.Same(obj2, result);

            result = cache.Get(key);
            Assert.Same(obj2, result);
        }

        [Fact]
        public void SetOverwritesAndInvokesCallbacks()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            var callback1Invoked = new ManualResetEvent(false);
            var callback2Invoked = new ManualResetEvent(false);

            var result = cache.Set(key, context =>
            {
                context.RegisterPostEvictionCallback((subkey, value, reason, state) =>
                {
                    Assert.Equal(key, subkey);
                    Assert.Same(value, obj);
                    Assert.Equal(EvictionReason.Replaced, reason);
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callback1Invoked);
                return obj;
            });
            Assert.Same(obj, result);

            var obj2 = new object();
            result = cache.Set(key, context =>
            {
                context.RegisterPostEvictionCallback((subkey, value, reason, state) =>
                {
                    // Shouldn't be invoked.
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callback2Invoked);
                return obj2;
            });
            Assert.Same(obj2, result);
            Assert.True(callback1Invoked.WaitOne(100), "Callback1");
            Assert.False(callback2Invoked.WaitOne(0), "Callback2");

            result = cache.Get(key);
            Assert.Same(obj2, result);

            Assert.False(callback2Invoked.WaitOne(0), "Callback2");
        }

        [Fact]
        public void RemoveRemoves()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            var result = cache.Set(key, obj);
            Assert.Same(obj, result);

            cache.Remove(key);
            result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public void RemoveRemovesAndInvokesCallback()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            var callbackInvoked = new ManualResetEvent(false);

            var result = cache.Set(key, context =>
            {
                context.RegisterPostEvictionCallback((subkey, value, reason, state) =>
                {
                    Assert.Equal(key, subkey);
                    Assert.Same(value, obj);
                    Assert.Equal(EvictionReason.Removed, reason);
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked);
                return obj;
            });
            Assert.Same(obj, result);

            cache.Remove(key);
            Assert.True(callbackInvoked.WaitOne(100), "Callback");

            result = cache.Get(key);
            Assert.Null(result);
        }

        [Fact]
        public void RemoveAndReAddFromCallbackWorks()
        {
            var cache = CreateCache();
            var obj = new object();
            var obj2 = new object();
            string key = "myKey";
            var callbackInvoked = new ManualResetEvent(false);

            var result = cache.Set(key, context =>
            {
                context.RegisterPostEvictionCallback((subkey, value, reason, state) =>
                {
                    Assert.Equal(key, subkey);
                    Assert.Same(value, obj);
                    Assert.Equal(EvictionReason.Removed, reason);
                    var localCallbackInvoked = (ManualResetEvent)state;
                    cache.Set(key, obj2);
                    localCallbackInvoked.Set();
                }, state: callbackInvoked);
                return obj;
            });
            Assert.Same(obj, result);

            cache.Remove(key);
            Assert.True(callbackInvoked.WaitOne(100), "Callback");

            result = cache.Get(key);
            Assert.Same(obj2, result);
        }
    }
}