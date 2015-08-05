// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Framework.Caching.Memory.Infrastructure;
using Microsoft.Framework.Internal;
using Xunit;

namespace Microsoft.Framework.Caching.Memory
{
    public class TriggeredExpirationTests
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
        public void SetWithTriggerRegistersForNotificaiton()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
            cache.Set(key, value, new MemoryCacheEntryOptions().AddExpirationTrigger(trigger));

            Assert.True(trigger.IsExpiredWasCalled);
            Assert.True(trigger.ActiveExpirationCallbacksWasCalled);
            Assert.NotNull(trigger.Registration);
            Assert.NotNull(trigger.Registration.RegisteredCallback);
            Assert.NotNull(trigger.Registration.RegisteredState);
            Assert.False(trigger.Registration.Disposed);
        }

        [Fact]
        public void SetWithLazyTriggerDoesntRegisterForNotification()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = false };
            cache.Set(key, value, new MemoryCacheEntryOptions().AddExpirationTrigger(trigger));

            Assert.True(trigger.IsExpiredWasCalled);
            Assert.True(trigger.ActiveExpirationCallbacksWasCalled);
            Assert.Null(trigger.Registration);
        }

        [Fact]
        public void FireTriggerRemovesItem()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationTrigger(trigger)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked));

            trigger.Fire();

            var found = cache.TryGetValue(key, out value);
            Assert.False(found);

            Assert.True(callbackInvoked.WaitOne(1000), "Callback");
        }

        [Fact]
        public void ExpiredLazyTriggerRemovesItemOnNextAccess()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = false };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationTrigger(trigger)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked));

            var found = cache.TryGetValue(key, out value);
            Assert.True(found);

            trigger.IsExpired = true;

            found = cache.TryGetValue(key, out value);
            Assert.False(found);

            Assert.True(callbackInvoked.WaitOne(1000), "Callback");
        }

        [Fact]
        public void ExpiredLazyTriggerRemovesItemInBackground()
        {
            var clock = new TestClock();
            var cache = CreateCache(clock);
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = false };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationTrigger(trigger)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
            {
                // TODO: Verify params
                var localCallbackInvoked = (ManualResetEvent)state;
                localCallbackInvoked.Set();
            }, state: callbackInvoked));
            var found = cache.TryGetValue(key, out value);
            Assert.True(found);

            clock.Add(TimeSpan.FromMinutes(2));
            trigger.IsExpired = true;
            var ignored = cache.Get("otherKey"); // Background expiration checks are triggered by misc cache activity.
            Assert.True(callbackInvoked.WaitOne(1000), "Callback");

            found = cache.TryGetValue(key, out value);
            Assert.False(found);
        }

        [Fact]
        public void RemoveItemDisposesTriggerRegistration()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
            cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationTrigger(trigger)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
            {
                // TODO: Verify params
                var localCallbackInvoked = (ManualResetEvent)state;
                localCallbackInvoked.Set();
            }, state: callbackInvoked));
            cache.Remove(key);

            Assert.NotNull(trigger.Registration);
            Assert.True(trigger.Registration.Disposed);
            Assert.True(callbackInvoked.WaitOne(1000), "Callback");
        }

        [Fact]
        public void AddExpiredTriggerPreventsCaching()
        {
            var cache = CreateCache();
            string key = "myKey";
            var value = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var trigger = new TestTrigger() { IsExpired = true };
            var result = cache.Set(key, value, new MemoryCacheEntryOptions()
                .AddExpirationTrigger(trigger)
                .RegisterPostEvictionCallback((subkey, subValue, reason, state) =>
            {
                // TODO: Verify params
                var localCallbackInvoked = (ManualResetEvent)state;
                localCallbackInvoked.Set();
            }, state: callbackInvoked));
            Assert.Same(value, result); // The created item should be returned, but not cached.

            Assert.True(trigger.IsExpiredWasCalled);
            Assert.False(trigger.ActiveExpirationCallbacksWasCalled);
            Assert.Null(trigger.Registration);
            Assert.True(callbackInvoked.WaitOne(1000), "Callback");

            result = cache.Get(key);
            Assert.Null(result); // It wasn't cached
        }
    }
}