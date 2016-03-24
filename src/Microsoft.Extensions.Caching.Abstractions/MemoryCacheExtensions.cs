// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory
{
    public static class CacheExtensions
    {

        public static object Get(this IMemoryCache cache, object key)
        {
            object value = null;
            cache.TryGetValue(key, out value);
            return value;
        }

        public static TItem Get<TItem>(this IMemoryCache cache, object key)
        {
            TItem value;
            cache.TryGetValue<TItem>(key, out value);
            return value;
        }

        public static bool TryGetValue<TItem>(this IMemoryCache cache, object key, out TItem value)
        {
            object result;
            if (cache.TryGetValue(key, out result))
            {
                value = (TItem)result;
                return true;
            }

            value = default(TItem);
            return false;
        }

        public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value)
        {
            using (var entry = cache.CreateEntry(key))
            {
                entry.Value = value;
            }

            return value;
        }

        public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, DateTimeOffset absoluteExpiration)
        {
            using (var entry = cache.CreateEntry(key))
            {
                entry.AbsoluteExpiration = absoluteExpiration;
                entry.Value = value;
            }

            return value;
        }

        public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, TimeSpan absoluteExpirationRelativeToNow)
        {
            using (var entry = cache.CreateEntry(key))
            {
                entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
                entry.Value = value;
            }

            return value;
        }

        public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, IChangeToken expirationToken)
        {
            using (var entry = cache.CreateEntry(key))
            {
                entry.AddExpirationToken(expirationToken);
                entry.Value = value;
            }

            return value;
        }

        public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, MemoryCacheEntryOptions options)
        {
            using (var entry = cache.CreateEntry(key))
            {
                if (options != null)
                {
                    entry.SetOptions(options);
                }

                entry.Value = value;
            }

            return value;
        }

        public static TItem GetOrCreate<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, TItem> factory)
        {
            object result;
            if (!cache.TryGetValue(key, out result))
            {
                using (var entry = cache.CreateEntry(key))
                {
                    result = factory(entry);
                    entry.SetValue(result);
                }
            }

            return (TItem)result;
        }

        public static async Task<TItem> GetOrCreateAsync<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, Task<TItem>> factory)
        {
            object result;
            if (!cache.TryGetValue(key, out result))
            {
                using (var entry = cache.CreateEntry(key))
                {
                    result = await factory(entry);
                    entry.SetValue(result);
                }
            }

            return (TItem)result;
        }
    }
}