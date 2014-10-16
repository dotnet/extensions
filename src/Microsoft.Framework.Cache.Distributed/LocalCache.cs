// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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

        public byte[] Set([NotNull] string key, object state, [NotNull] Func<ICacheContext, byte[]> create)
        {

            return _memCache.Set<byte[]>(key, state, context =>
            {
                var subContext = new LocalContextWrapper(context);
                return create(subContext);
            });
        }

        public bool TryGetValue([NotNull] string key, out byte[] value)
        {
            return _memCache.TryGetValue(key, out value);
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