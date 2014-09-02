// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNet.MemoryCache.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.MemoryCache
{
    public class TriggeredExpirationTests
    {
        [Fact]
        public void SetWithTriggerRegistersForNotificaiton()
        {
            var cache = new MemoryCache();
            string key = "myKey";
            var obj = new object();
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
            cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                return obj;
            });

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
            var cache = new MemoryCache();
            string key = "myKey";
            var obj = new object();
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = false };
            cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                return obj;
            });

            Assert.True(trigger.IsExpiredWasCalled);
            Assert.True(trigger.ActiveExpirationCallbacksWasCalled);
            Assert.Null(trigger.Registration);
        }

        [Fact]
        public void FireTriggerRemovesItem()
        {
            var cache = new MemoryCache();
            string key = "myKey";
            var obj = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
            cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                context.RegisterPostEvictionCallback((subkey, value, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked);
                return obj;
            });

            trigger.Fire();

            var found = cache.TryGetValue(key, out obj);
            Assert.False(found);

            Assert.True(callbackInvoked.WaitOne(100), "Callback");
        }

        [Fact]
        public void ExpiredLazyTriggerRemovesItemOnNextAccess()
        {
            var cache = new MemoryCache();
            string key = "myKey";
            var obj = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = false };
            cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                context.RegisterPostEvictionCallback((subkey, value, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked);
                return obj;
            });
            var found = cache.TryGetValue(key, out obj);
            Assert.True(found);

            trigger.IsExpired = true;

            found = cache.TryGetValue(key, out obj);
            Assert.False(found);

            Assert.True(callbackInvoked.WaitOne(100), "Callback");
        }

        [Fact]
        public void RemoveItemDisposesTriggerRegistration()
        {
            var cache = new MemoryCache();
            string key = "myKey";
            var obj = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var trigger = new TestTrigger() { ActiveExpirationCallbacks = true };
            cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                context.RegisterPostEvictionCallback((subkey, value, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked);
                return obj;
            });
            cache.Remove(key);

            Assert.NotNull(trigger.Registration);
            Assert.True(trigger.Registration.Disposed);
            Assert.True(callbackInvoked.WaitOne(100), "Callback");
        }

        [Fact]
        public void AddExpiredTriggerPreventsCaching()
        {
            var cache = new MemoryCache();
            string key = "myKey";
            var obj = new object();
            var callbackInvoked = new ManualResetEvent(false);
            var trigger = new TestTrigger() { IsExpired = true };
            var result = cache.Set(key, context =>
            {
                context.AddExpirationTrigger(trigger);
                context.RegisterPostEvictionCallback((subkey, value, reason, state) =>
                {
                    // TODO: Verify params
                    var localCallbackInvoked = (ManualResetEvent)state;
                    localCallbackInvoked.Set();
                }, state: callbackInvoked);
                return obj;
            });
            Assert.Same(obj, result); // The created item should be returned, but not cached.

            Assert.True(trigger.IsExpiredWasCalled);
            Assert.False(trigger.ActiveExpirationCallbacksWasCalled);
            Assert.Null(trigger.Registration);
            Assert.True(callbackInvoked.WaitOne(100), "Callback");

            result = cache.Get(key);
            Assert.Null(result); // It wasn't cached
        }
    }
}