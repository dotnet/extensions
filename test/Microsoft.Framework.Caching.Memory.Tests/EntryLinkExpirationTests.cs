// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Caching.Memory.Infrastructure;
using Xunit;

namespace Microsoft.Framework.Caching.Memory
{
    public class EntryLinkExpirationTests
    {
        private IMemoryCache CreateCache()
        {
            return CreateCache(new SystemClock());
        }

        private IMemoryCache CreateCache(ISystemClock clock)
        {
            return new MemoryCache(new MemoryCacheOptions()
            {
                Clock = clock,
                CompactOnMemoryPressure = false,
            });
        }

        [Fact]
        public void SetPopulates_Triggers_IntoScopedLink()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";

            IEntryLink linkScope1;
            using (linkScope1 = cache.CreateLinkingScope())
            {
                Assert.Same(linkScope1, EntryLinkHelpers.ContextLink);

                var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
                cache.Set(key, obj, new MemoryCacheEntryOptions().AddExpirationTrigger(trigger));
            }

            Assert.Equal(1, linkScope1.Triggers.Count());
            Assert.Null(linkScope1.AbsoluteExpiration);
        }

        [Fact]
        public void SetPopulates_AbsoluteExpiration_IntoScopeLink()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            var time = new DateTimeOffset(2051, 1, 1, 1, 1, 1, TimeSpan.Zero);

            IEntryLink linkScope1;
            using (linkScope1 = cache.CreateLinkingScope())
            {
                Assert.Same(linkScope1, EntryLinkHelpers.ContextLink);

                var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
                cache.Set(key, obj, new MemoryCacheEntryOptions().SetAbsoluteExpiration(time));
            }

            Assert.Equal(0, linkScope1.Triggers.Count());
            Assert.NotNull(linkScope1.AbsoluteExpiration);
            Assert.Equal(time, linkScope1.AbsoluteExpiration);
        }

        [Fact]
        public void TriggerExpires_LinkedEntry()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };

            using (var link = cache.CreateLinkingScope())
            {
                cache.Set(key, obj, new MemoryCacheEntryOptions().AddExpirationTrigger(trigger));

                cache.Set(key1, obj, new MemoryCacheEntryOptions().AddEntryLink(link));
            }

            Assert.StrictEqual(obj, cache.Get(key));
            Assert.StrictEqual(obj, cache.Get(key1));

            trigger.Fire();

            object value;
            Assert.False(cache.TryGetValue(key1, out value));
            Assert.False(cache.TryGetValue(key, out value));
        }

        [Fact]
        public void AbsoluteExpiration_WorksAcrossLink()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };

            using (var link = cache.CreateLinkingScope())
            {
                cache.Set(key, obj, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(5)));

                cache.Set(key1, obj, new MemoryCacheEntryOptions().AddEntryLink(link));
            }

            Assert.StrictEqual(obj, cache.Get(key));
            Assert.StrictEqual(obj, cache.Get(key1));

            clock.Add(TimeSpan.FromSeconds(10));

            object value;
            Assert.False(cache.TryGetValue(key1, out value));
            Assert.False(cache.TryGetValue(key, out value));
        }

        [Fact]
        public void GetWithImplicitLinkPopulatesTriggers()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";

            Assert.Null(EntryLinkHelpers.ContextLink);

            IEntryLink link;
            using (link = cache.CreateLinkingScope())
            {
                Assert.StrictEqual(link, EntryLinkHelpers.ContextLink);
                var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
                cache.Set(key, obj, new MemoryCacheEntryOptions().AddExpirationTrigger(trigger));
            }

            Assert.Null(EntryLinkHelpers.ContextLink);

            Assert.Equal(1, link.Triggers.Count());
            Assert.Null(link.AbsoluteExpiration);

            cache.Set(key1, obj, new MemoryCacheEntryOptions().AddEntryLink(link));
        }

        [Fact]
        public void LinkContextsCanNest()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";

            Assert.Null(EntryLinkHelpers.ContextLink);

            IEntryLink link1;
            IEntryLink link2;
            using (link1 = cache.CreateLinkingScope())
            {
                Assert.StrictEqual(link1, EntryLinkHelpers.ContextLink);

                using (link2 = cache.CreateLinkingScope())
                {
                    Assert.StrictEqual(link2, EntryLinkHelpers.ContextLink);

                    var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
                    cache.Set(key, obj, new MemoryCacheEntryOptions().AddExpirationTrigger(trigger));
                }

                Assert.StrictEqual(link1, EntryLinkHelpers.ContextLink);
            }

            Assert.Null(EntryLinkHelpers.ContextLink);

            Assert.Equal(0, link1.Triggers.Count());
            Assert.Null(link1.AbsoluteExpiration);
            Assert.Equal(1, link2.Triggers.Count());
            Assert.Null(link2.AbsoluteExpiration);

            cache.Set(key1, obj, new MemoryCacheEntryOptions().AddEntryLink(link2));
        }

        [Fact]
        public void NestedLinkContextsCanAggregate()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var obj = new object();
            string key2 = "myKey2";
            string key3 = "myKey3";

            var trigger2 = new TestTrigger() { ActiveExpirationCallbacks = true };
            var trigger3 = new TestTrigger() { ActiveExpirationCallbacks = true };

            IEntryLink link1 = null;
            IEntryLink link2 = null;

            using (link1 = cache.CreateLinkingScope())
            {
                cache.Set(key2, obj, new MemoryCacheEntryOptions()
                    .AddExpirationTrigger(trigger2)
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(10)));

                using (link2 = cache.CreateLinkingScope())
                {
                    cache.Set(key3, obj, new MemoryCacheEntryOptions()
                        .AddExpirationTrigger(trigger3)
                        .SetAbsoluteExpiration(TimeSpan.FromSeconds(15)));
                }
            }

            Assert.Equal(1, link1.Triggers.Count());
            Assert.NotNull(link1.AbsoluteExpiration);
            Assert.Equal(clock.UtcNow + TimeSpan.FromSeconds(10), link1.AbsoluteExpiration);

            Assert.Equal(1, link2.Triggers.Count());
            Assert.NotNull(link2.AbsoluteExpiration);
            Assert.Equal(clock.UtcNow + TimeSpan.FromSeconds(15), link2.AbsoluteExpiration);
        }
    }
}