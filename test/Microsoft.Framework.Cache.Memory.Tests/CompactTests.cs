// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Cache.Memory.Infrastructure;
using Xunit;

namespace Microsoft.Framework.Cache.Memory
{
    public class CompactTests
    {
        private MemoryCache CreateCache()
        {
            return new MemoryCache(new MemoryCacheOptions()
            {
                ListenForMemoryPressure = false,
            });
        }

        [Fact]
        public void CompactEmptyNoOps()
        {
            var cache = CreateCache();
            cache.Compact(0.10);
        }

        [Fact]
        public void Compact100PercentClearsAll()
        {
            var cache = CreateCache();
            cache.Set("key1", "value1");
            cache.Set("key2", "value2");
            Assert.Equal(2, cache.Count);
            cache.Compact(1.0);
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void Compact100PercentClearsAllButNeverRemoveItems()
        {
            var cache = CreateCache();
            cache.Set("key1", context =>
            {
                context.SetPriority(CachePreservationPriority.NeverRemove);
                return "Value1";
            });
            cache.Set("key2", context =>
            {
                context.SetPriority(CachePreservationPriority.NeverRemove);
                return "Value2";
            });
            cache.Set("key3", "value3");
            cache.Set("key4", "value4");
            Assert.Equal(4, cache.Count);
            cache.Compact(1.0);
            Assert.Equal(2, cache.Count);
            Assert.Equal("Value1", cache.Get("key1"));
            Assert.Equal("Value2", cache.Get("key2"));
        }

        [Fact]
        public void CompactPrioritizesLowPriortyItems()
        {
            var cache = CreateCache();
            cache.Set("key1", context =>
            {
                context.SetPriority(CachePreservationPriority.Low);
                return "Value1";
            });
            cache.Set("key2", context =>
            {
                context.SetPriority(CachePreservationPriority.Low);
                return "Value2";
            });
            cache.Set("key3", "value3");
            cache.Set("key4", "value4");
            Assert.Equal(4, cache.Count);
            cache.Compact(0.5);
            Assert.Equal(2, cache.Count);
            Assert.Equal("value3", cache.Get("key3"));
            Assert.Equal("value4", cache.Get("key4"));
        }

        [Fact]
        public void CompactPrioritizesLRU()
        {
            var cache = CreateCache();
            cache.Set("key1", "value1");
            cache.Set("key2", "value2");
            cache.Set("key3", "value3");
            cache.Set("key4", "value4");
            Assert.Equal(4, cache.Count);
            cache.Compact(0.90);
            Assert.Equal(1, cache.Count);
            Assert.Equal("value4", cache.Get("key4"));
        }
    }
}