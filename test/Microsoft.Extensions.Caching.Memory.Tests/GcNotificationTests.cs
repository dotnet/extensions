// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.Extensions.Caching.Memory
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

            GcCollectAndWait();
            Assert.True(callbackInvoked.WaitOne(0));
        }

        [Fact]
        public void CallbackInvokedMultipleTimes()
        {
            var reRegisterForFinalize = true;
            var callbackInvoked = new ManualResetEvent(false);
            GcNotification.Register(state =>
            {
                callbackInvoked.Set();
                return reRegisterForFinalize;
            }, null);

            GcCollectAndWait();
            Assert.True(callbackInvoked.WaitOne(0));

            callbackInvoked.Reset();
            reRegisterForFinalize = false;

            GcCollectAndWait();
            Assert.True(callbackInvoked.WaitOne(0));

            callbackInvoked.Reset();

            // No callback expected the 3rd time
            GcCollectAndWait();
            Assert.False(callbackInvoked.WaitOne(0));
        }

        private static void GcCollectAndWait()
        {
            // We need to collect twice for this test to work on Mono
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();
        }
    }
}