// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Services;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Semantic
{
    public class MemoryCacheTest
    {
        [Fact]
        public void LastAccessIsUpdated()
        {
            var cache = new TestMemoryCache();
            var key = GetKey();
            var value = new List<uint>();

            cache.Set(key, value);
            var oldAccessTime = cache.GetAccessTime(key);

            Thread.Sleep(millisecondsTimeout: 10);

            cache.Get(key);
            var newAccessTime = cache.GetAccessTime(key);

            Assert.True(newAccessTime > oldAccessTime, "New AccessTime should be greater than old");
        }

        [Fact]
        public void BasicAdd()
        {
            var cache = new TestMemoryCache();
            var key = GetKey();
            var value = new List<uint> { 1, 2, 3 };

            cache.Set(key, value);

            var result = cache.Get(key);

            Assert.Same(value, result);
        }

        [Fact]
        public void Compaction()
        {
            var cache = new TestMemoryCache();
            var sizeLimit = TestMemoryCache.DefaultSizeLimit;

            for(var i = 0; i < sizeLimit; i++)
            {
                var key = GetKey();
                var value = new List<uint> { (uint)i };
                cache.Set(key, value);
                Assert.False(cache.WasCompacted, "It got compacted early.");
            }

            cache.Set(GetKey(), new List<uint> { (uint)sizeLimit + 1 });
            Assert.True(cache.WasCompacted, "Compaction is not happening");
        }

        private static string GetKey()
        {
            return Guid.NewGuid().ToString();
        }

        private class TestMemoryCache : MemoryCache<IReadOnlyList<uint>>
        {
            public static int DefaultSizeLimit = 10;

            protected override int SizeLimit => DefaultSizeLimit;

            public bool WasCompacted = false;

            public DateTime GetAccessTime(string key)
            {
                return _dict[key].LastAccess;
            }

            protected override void Compact()
            {
                WasCompacted = true;
                base.Compact();
            }
        }
    }
}
