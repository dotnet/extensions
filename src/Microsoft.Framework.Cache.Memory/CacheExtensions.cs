// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Cache.Memory
{
    public static class CacheExtensions
    {
        public static object Set(this IMemoryCache cache, string key, object obj)
        {
            return cache.Set(key, EntryLinkHelpers.ContextLink, state: obj, create: context => context.State);
        }

        public static T Set<T>(this IMemoryCache cache, string key, T obj)
        {
            return (T)cache.Set(key, (object)obj);
        }

        public static object Set(this IMemoryCache cache, string key, Func<ICacheSetContext, object> create)
        {
            return cache.Set(key, EntryLinkHelpers.ContextLink, create);
        }

        public static object Set(this IMemoryCache cache, string key, IEntryLink link, Func<ICacheSetContext, object> create)
        {
            return cache.Set(key, link, state: null, create: create);
        }

        public static object Set(this IMemoryCache cache, string key, object state, Func<ICacheSetContext, object> create)
        {
            return cache.Set(key, EntryLinkHelpers.ContextLink, state, create);
        }

        public static T Set<T>(this IMemoryCache cache, string key, Func<ICacheSetContext, T> create)
        {
            return (T)cache.Set(key, create, context =>
            {
                var myCreate = (Func<ICacheSetContext, T>)context.State;
                return (object)myCreate(context);
            });
        }

        public static T Set<T>(this IMemoryCache cache, string key, IEntryLink link, Func<ICacheSetContext, T> create)
        {
            return (T)cache.Set(key, link, create, context =>
            {
                var myCreate = (Func<ICacheSetContext, T>)context.State;
                return (object)myCreate(context);
            });
        }

        public static T Set<T>(this IMemoryCache cache, string key, object state, Func<ICacheSetContext, T> create)
        {
            return (T)cache.Set(key, state, context =>
            {
                return (object)create(context);
            });
        }

        public static T Set<T>(this IMemoryCache cache, string key, IEntryLink link, object state, Func<ICacheSetContext, T> create)
        {
            return (T)cache.Set(key, link, state, context =>
            {
                return (object)create(context);
            });
        }

        public static object Get(this IMemoryCache cache, string key)
        {
            object value = null;
            cache.TryGetValue(key, out value);
            return value;
        }

        public static object Get(this IMemoryCache cache, string key, IEntryLink link)
        {
            object value = null;
            cache.TryGetValue(key, link, out value);
            return value;
        }

        public static T Get<T>(this IMemoryCache cache, string key)
        {
            T value = default(T);
            cache.TryGetValue<T>(key, out value);
            return value;
        }

        public static T Get<T>(this IMemoryCache cache, string key, IEntryLink link)
        {
            T value = default(T);
            cache.TryGetValue<T>(key, link, out value);
            return value;
        }

        public static bool TryGetValue<T>(this IMemoryCache cache, string key, out T value)
        {
            object obj = null;
            if (cache.TryGetValue(key, EntryLinkHelpers.ContextLink, out obj))
            {
                value = (T)obj;
                return true;
            }
            value = default(T);
            return false;
        }

        public static bool TryGetValue<T>(this IMemoryCache cache, string key, IEntryLink link, out T value)
        {
            object obj = null;
            if (cache.TryGetValue(key, link, out obj))
            {
                value = (T)obj;
                return true;
            }
            value = default(T);
            return false;
        }

        public static object GetOrSet(this IMemoryCache cache, string key, object value)
        {
            object obj;
            if (cache.TryGetValue(key, out obj))
            {
                return obj;
            }
            return cache.Set(key, value);
        }

        public static object GetOrSet(this IMemoryCache cache, string key, Func<ICacheSetContext, object> create)
        {
            object obj;
            if (cache.TryGetValue(key, out obj))
            {
                return obj;
            }
            return cache.Set(key, state: null, create: create);
        }

        public static object GetOrSet(this IMemoryCache cache, string key, IEntryLink link, Func<ICacheSetContext, object> create)
        {
            object obj;
            if (cache.TryGetValue(key, link, out obj))
            {
                return obj;
            }
            return cache.Set(key, link, create);
        }

        public static object GetOrSet(this IMemoryCache cache, string key, object state, Func<ICacheSetContext, object> create)
        {
            object obj;
            if (cache.TryGetValue(key, out obj))
            {
                return obj;
            }
            return cache.Set(key, state, create);
        }

        public static object GetOrSet(this IMemoryCache cache, string key, IEntryLink link, object state, Func<ICacheSetContext, object> create)
        {
            object obj;
            if (cache.TryGetValue(key, link, out obj))
            {
                return obj;
            }
            return cache.Set(key, link, state, create);
        }

        public static T GetOrSet<T>(this IMemoryCache cache, string key, Func<ICacheSetContext, T> create)
        {
            T obj;
            if (cache.TryGetValue(key, out obj))
            {
                return obj;
            }
            return cache.Set(key, create);
        }

        public static T GetOrSet<T>(this IMemoryCache cache, string key, IEntryLink link, Func<ICacheSetContext, T> create)
        {
            T obj;
            if (cache.TryGetValue(key, link, out obj))
            {
                return obj;
            }
            return cache.Set(key, link, create);
        }

        public static T GetOrSet<T>(this IMemoryCache cache, string key, object state, Func<ICacheSetContext, T> create)
        {
            T obj;
            if (cache.TryGetValue(key, out obj))
            {
                return obj;
            }
            return cache.Set(key, state, create);
        }

        public static T GetOrSet<T>(this IMemoryCache cache, string key, IEntryLink link, object state, Func<ICacheSetContext, T> create)
        {
            T obj;
            if (cache.TryGetValue(key, link, out obj))
            {
                return obj;
            }
            return cache.Set(key, link, state, create);
        }

        /// <summary>
        /// Adds inherited trigger and absolute expiration information.
        /// </summary>
        /// <param name="link"></param>
        public static void AddEntryLink(this ICacheSetContext context, IEntryLink link)
        {
            foreach (var trigger in link.Triggers)
            {
                context.AddExpirationTrigger(trigger);
            }

            if (link.AbsoluteExpiration.HasValue)
            {
                context.SetAbsoluteExpiration(link.AbsoluteExpiration.Value);
            }
        }
    }
}