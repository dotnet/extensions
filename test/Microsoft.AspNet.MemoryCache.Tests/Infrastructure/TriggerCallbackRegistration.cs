// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.MemoryCache.Infrastructure
{
    public class TriggerCallbackRegistration : IDisposable
    {
        public Action<object> RegisteredCallback { get; set; }

        public object RegisteredState { get; set; }

        public bool Disposed { get; set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}