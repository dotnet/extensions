// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Xunit;

namespace Microsoft.Framework.Cache.Memory.Infrastructure
{
    public class GcNotificationTests
    {
        [Fact]
        public void CallbackRegisteredAndInvoked()
        {
            var callbackInvoked = new ManualResetEvent(false);
            GcNotification.Register(state =>
            {
                callbackInvoked.Set();
                return false;
            }, null);

            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Assert.True(callbackInvoked.WaitOne(100));
        }

        [Fact]
        public void CallbackInvokedMultipleTimes()
        {
            int callbackCount = 0;
            var callbackInvoked = new ManualResetEvent(false);
            GcNotification.Register(state =>
            {
                callbackCount++;
                callbackInvoked.Set();
                if (callbackCount < 2)
                {
                    return true;
                }
                return false;
            }, null);

            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Assert.True(callbackInvoked.WaitOne(100));
            Assert.Equal(1, callbackCount);

            callbackInvoked.Reset();

            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Assert.True(callbackInvoked.WaitOne(100));
            Assert.Equal(2, callbackCount);

            callbackInvoked.Reset();

            // No callback expected the 3rd time
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            Assert.False(callbackInvoked.WaitOne(100));
            Assert.Equal(2, callbackCount);
        }
    }
}