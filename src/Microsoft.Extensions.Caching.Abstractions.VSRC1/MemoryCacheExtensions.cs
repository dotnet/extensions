// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Caching.Memory.VSRC1
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
            object obj = null;
            if (cache.TryGetValue(key, out obj))
            {
                value = (TItem)obj;
                return true;
            }
            value = default(TItem);
            return false;
        }

        public static object Set(this IMemoryCache cache, object key, object value)
        {
            return cache.Set(key, value, new MemoryCacheEntryOptions());
        }

        public static object Set(this IMemoryCache cache, object key, object value, MemoryCacheEntryOptions options)
        {
            return cache.Set(key, value, options);
        }

        public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value)
        {
            return (TItem)cache.Set(key, (object)value, new MemoryCacheEntryOptions());
        }

        public static TItem Set<TItem>(this IMemoryCache cache, object key, TItem value, MemoryCacheEntryOptions options)
        {
            return (TItem)cache.Set(key, (object)value, options);
        }
    }
}