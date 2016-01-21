// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders
{
    internal class NoopChangeToken : IChangeToken
    {
        public static NoopChangeToken Singleton { get; } = new NoopChangeToken();

        private NoopChangeToken()
        {
        }

        public bool HasChanged => false;

        public bool ActiveChangeCallbacks => false;

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            return EmptyDisposable.Instance;
        }
    }
}