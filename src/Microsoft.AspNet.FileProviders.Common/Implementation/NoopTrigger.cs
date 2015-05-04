// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Caching;

namespace Microsoft.AspNet.FileProviders
{
    internal class NoopTrigger : IExpirationTrigger
    {
        public static NoopTrigger Singleton { get; } = new NoopTrigger();

        private NoopTrigger()
        {
        }

        public bool ActiveExpirationCallbacks
        {
            get { return false; }
        }

        public bool IsExpired
        {
            get { return false; }
        }

        public IDisposable RegisterExpirationCallback(Action<object> callback, object state)
        {
            throw new InvalidOperationException("Trigger does not support registering change notifications.");
        }
    }
}