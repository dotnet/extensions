// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Test
{
    public class MemoryCacheTest
    {
        [Fact]
        public async Task ConcurrentSets_DoesNotThrow()
        {
            // Arrange
            var cache = new TestMemoryCache();
            var entries = Enumerable.Range(0, 500);
            var repeatCount = 4;

            // 1111 2222 3333 4444 ...
            var repeatedEntries = entries.SelectMany(entry => Enumerable.Repeat(entry, repeatCount));
            var tasks = repeatedEntries.Select(async entry =>
            {
                // 2 is an arbitrarily low number, we're just trying to emulate concurrency
                await Task.Delay(2);
                cache.Set(entry.ToString(CultureInfo.InvariantCulture), Array.Empty<uint>());
            });

            // Act & Assert
            await Task.WhenAll(tasks);
        }

        [Fact]
        public void LastAccessIsUpdated()
        {
            var cache = new TestMemoryCache();
            var key = GetKey();
            var value = new List<uint>();

            cache.Set(key, value);
            var oldAccessTime = cache.GetAccessTime(key);

            Thread.Sleep(millisecondsTimeout: 10);

            cache.TryGetValue(key, out _);
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

            cache.TryGetValue(key, out var result);

            Assert.Same(value, result);
        }

        [Fact]
        public void Compaction()
        {
            var cache = new TestMemoryCache();
            var sizeLimit = TestMemoryCache.SizeLimit;

            for (var i = 0; i < sizeLimit; i++)
            {
                var key = GetKey();
                var value = new List<uint> { (uint)i };
                cache.Set(key, value);
                Assert.False(cache.WasCompacted, "It got compacted early.");
            }

            cache.Set(GetKey(), new List<uint> { (uint)sizeLimit + 1 });
            Assert.True(cache.WasCompacted, "Compaction is not happening");
        }

        [Fact]
        public void MissingKey()
        {
            var cache = new TestMemoryCache();
            var key = GetKey();

            cache.TryGetValue(key, out var value);

            Assert.Null(value);
        }

        [Fact]
        public void NullKey()
        {
            var cache = new TestMemoryCache();

            Assert.Throws<ArgumentNullException>(() => cache.TryGetValue(key: null, out var result));
        }

        private static string GetKey()
        {
            return Guid.NewGuid().ToString();
        }

        private class TestMemoryCache : MemoryCache<string, IReadOnlyList<uint>>
        {
            public static int SizeLimit = 10;
            public bool WasCompacted = false;

            public TestMemoryCache() : base(SizeLimit)
            {
            }

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
