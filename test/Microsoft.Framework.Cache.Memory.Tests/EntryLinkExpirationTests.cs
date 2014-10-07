// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Framework.Cache.Memory.Infrastructure;
using Xunit;

namespace Microsoft.Framework.Cache.Memory.Tests
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
                ListenForMemoryPressure = false,
            });
        }

        [Fact]
        public void GetWithLinkPopulatesTriggers()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";

            var link = new EntryLink();

            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
            cache.Set(key, link, context =>
            {
                context.AddExpirationTrigger(trigger);
                return obj;
            });

            Assert.Equal(1, link.Triggers.Count());
            Assert.Null(link.AbsoluteExpiration);

            cache.Set(key1, context =>
            {
                context.AddEntryLink(link);
                return obj;
            });
        }

        [Fact]
        public void GetWithLinkPopulatesAbsoluteExpiration()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";

            var link = new EntryLink();

            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
            var time = new DateTimeOffset(2051, 1, 1, 1, 1, 1, TimeSpan.Zero);
            cache.Set(key, link, context =>
            {
                context.SetAbsoluteExpiration(time);
                return obj;
            });

            Assert.Equal(0, link.Triggers.Count());
            Assert.NotNull(link.AbsoluteExpiration);
            Assert.Equal(time, link.AbsoluteExpiration);

            cache.Set(key1, context =>
            {
                context.AddEntryLink(link);
                return obj;
            });
        }

        [Fact]
        public void TriggerExpiresLinkedEntry()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";

            var link = new EntryLink();

            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
            cache.Set(key, link, context =>
            {
                context.AddExpirationTrigger(trigger);
                return obj;
            });

            cache.Set(key1, context =>
            {
                context.AddEntryLink(link);
                return obj;
            });

            Assert.StrictEqual(obj, cache.Get(key));
            Assert.StrictEqual(obj, cache.Get(key1));

            trigger.Fire();

            object value;
            Assert.False(cache.TryGetValue(key1, out value));
            Assert.False(cache.TryGetValue(key, out value));
        }

        [Fact]
        public void AbsoluteExpirationWorksAcrossLink()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";

            var link = new EntryLink();

            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
            cache.Set(key, link, context =>
            {
                context.SetAbsoluteExpiration(TimeSpan.FromSeconds(5));
                return obj;
            });

            cache.Set(key1, context =>
            {
                context.AddEntryLink(link);
                return obj;
            });

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

            var link = new EntryLink();

            Assert.Null(EntryLinkHelpers.ContextLink);

            using (link.FlowContext())
            {
                Assert.StrictEqual(link, EntryLinkHelpers.ContextLink);
                var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
                cache.Set(key, context =>
                {
                    context.AddExpirationTrigger(trigger);
                    return obj;
                });
            }

            Assert.Null(EntryLinkHelpers.ContextLink);

            Assert.Equal(1, link.Triggers.Count());
            Assert.Null(link.AbsoluteExpiration);

            cache.Set(key1, context =>
            {
                context.AddEntryLink(link);
                return obj;
            });
        }

        [Fact]
        public void LinkContextsCanNest()
        {
            var cache = CreateCache();
            var obj = new object();
            string key = "myKey";
            string key1 = "myKey1";

            var link1 = new EntryLink();
            var link2 = new EntryLink();

            Assert.Null(EntryLinkHelpers.ContextLink);

            using (link1.FlowContext())
            {
                Assert.StrictEqual(link1, EntryLinkHelpers.ContextLink);

                using (link2.FlowContext())
                {
                    Assert.StrictEqual(link2, EntryLinkHelpers.ContextLink);

                    var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
                    cache.Set(key, context =>
                    {
                        context.AddExpirationTrigger(trigger);
                        return obj;
                    });
                }

                Assert.StrictEqual(link1, EntryLinkHelpers.ContextLink);
            }

            Assert.Null(EntryLinkHelpers.ContextLink);

            Assert.Equal(0, link1.Triggers.Count());
            Assert.Null(link1.AbsoluteExpiration);
            Assert.Equal(1, link2.Triggers.Count());
            Assert.Null(link2.AbsoluteExpiration);

            cache.Set(key1, context =>
            {
                context.AddEntryLink(link2);
                return obj;
            });
        }

        [Fact]
        public void NestedLinkContextsCanAggregate()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            var obj = new object();
            string key1 = "myKey1";
            string key2 = "myKey2";
            string key3 = "myKey3";

            var link1 = new EntryLink();
            var link2 = new EntryLink();

            var trigger2 = new TestTrigger() { ActiveExpirationCallbacks = true };
            var trigger3 = new TestTrigger() { ActiveExpirationCallbacks = true };

            cache.GetOrSet(key1, context1 =>
            {
                using (link1.FlowContext())
                {
                    cache.GetOrSet(key2, context2 =>
                    {
                        context2.AddExpirationTrigger(trigger2);
                        context2.SetAbsoluteExpiration(TimeSpan.FromSeconds(10));

                        using (link2.FlowContext())
                        {
                            cache.GetOrSet(key3, context3 =>
                            {
                                context3.AddExpirationTrigger(trigger3);
                                context3.SetAbsoluteExpiration(TimeSpan.FromSeconds(15));
                                return obj;
                            });
                        }
                        context2.AddEntryLink(link2);
                        return obj;
                    });
                }
                context1.AddEntryLink(link1);
                return obj;
            });

            Assert.Equal(2, link1.Triggers.Count());
            Assert.NotNull(link1.AbsoluteExpiration);
            Assert.Equal(clock.UtcNow + TimeSpan.FromSeconds(10), link1.AbsoluteExpiration);

            Assert.Equal(1, link2.Triggers.Count());
            Assert.NotNull(link2.AbsoluteExpiration);
            Assert.Equal(clock.UtcNow + TimeSpan.FromSeconds(15), link2.AbsoluteExpiration);

            cache.Set(key1, context =>
            {
                context.AddEntryLink(link2);
                return obj;
            });
        }
    }
}