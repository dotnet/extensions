// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.Cache.Memory;

namespace Microsoft.Framework.Cache.Distributed
{
    public class LocalCache : IDistributedCache
    {
        private readonly IMemoryCache _memCache;

        public LocalCache([NotNull] IMemoryCache memoryCache)
        {
            _memCache = memoryCache;
        }

        public void Connect()
        {
        }

        public Stream Set([NotNull] string key, object state, [NotNull] Action<ICacheContext> create)
        {
            var data = _memCache.Set<byte[]>(key, state, context =>
            {
                var subContext = new LocalContextWrapper(context);
                create(subContext);
                return subContext.GetBytes();
            });
            return new MemoryStream(data, writable: false);
        }

        public bool TryGetValue([NotNull] string key, out Stream value)
        {
            byte[] data;
            if (_memCache.TryGetValue(key, out data))
            {
                value = new MemoryStream(data, writable: false);
                return true;
            }
            value = null;
            return false;
        }

        public void Refresh([NotNull] string key)
        {
            object value;
            _memCache.TryGetValue(key, out value);
        }

        public void Remove([NotNull] string key)
        {
            _memCache.Remove(key);
        }
    }
}