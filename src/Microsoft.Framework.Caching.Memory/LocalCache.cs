// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.Caching.Distributed
{
    public class LocalCache : IDistributedCache
    {
        private static readonly Task CompletedTask = Task.FromResult<object>(null);

        private readonly IMemoryCache _memCache;

        public LocalCache([NotNull] IMemoryCache memoryCache)
        {
            _memCache = memoryCache;
        }

        public void Connect()
        {
        }

        public Task ConnectAsync()
        {
            return CompletedTask;
        }

        public byte[] Get([NotNull] string key)
        {
            return (byte[])_memCache.Get(key);
        }

        public Task<byte[]> GetAsync([NotNull] string key)
        {
            return Task.FromResult(Get(key));
        }

        public void Set([NotNull] string key, byte[] value, DistributedCacheEntryOptions options)
        {
            var memoryCacheEntryOptions = new MemoryCacheEntryOptions();
            memoryCacheEntryOptions.AbsoluteExpiration = options.AbsoluteExpiration;
            memoryCacheEntryOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
            memoryCacheEntryOptions.SlidingExpiration = options.SlidingExpiration;

            _memCache.Set(key, value, memoryCacheEntryOptions);
        }

        public Task SetAsync([NotNull] string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Set(key, value, options);
            return CompletedTask;
        }

        public void Refresh([NotNull] string key)
        {
            object value;
            _memCache.TryGetValue(key, out value);
        }

        public Task RefreshAsync([NotNull] string key)
        {
            Refresh(key);
            return CompletedTask;
        }

        public void Remove([NotNull] string key)
        {
            _memCache.Remove(key);
        }

        public Task RemoveAsync([NotNull] string key)
        {
            Remove(key);
            return CompletedTask;
        }
    }
}