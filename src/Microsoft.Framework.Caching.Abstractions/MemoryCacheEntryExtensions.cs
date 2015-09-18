// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Caching.Memory
{
    public static class MemoryCacheEntryExtensions
    {
        /// <summary>
        /// Sets the priority for keeping the cache entry in the cache during a memory pressure triggered cleanup.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="priority"></param>
        public static MemoryCacheEntryOptions SetPriority(
            this MemoryCacheEntryOptions options,
            CacheItemPriority priority)
        {
            options.Priority = priority;
            return options;
        }

        /// <summary>
        /// Expire the cache entry if the given event occurs.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="trigger"></param>
        public static MemoryCacheEntryOptions AddExpirationTrigger(
            this MemoryCacheEntryOptions options,
            IExpirationTrigger trigger)
        {
            if (trigger == null)
            {
                throw new ArgumentNullException(nameof(trigger));
            }

            options.Triggers.Add(trigger);
            return options;
        }

        /// <summary>
        /// Sets an absolute expiration time, relative to now.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="relative"></param>
        public static MemoryCacheEntryOptions SetAbsoluteExpiration(
            this MemoryCacheEntryOptions options,
            TimeSpan relative)
        {
            options.AbsoluteExpirationRelativeToNow = relative;
            return options;
        }

        /// <summary>
        /// Sets an absolute expiration date for the cache entry.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="absolute"></param>
        public static MemoryCacheEntryOptions SetAbsoluteExpiration(
            this MemoryCacheEntryOptions options,
            DateTimeOffset absolute)
        {
            options.AbsoluteExpiration = absolute;
            return options;
        }

        /// <summary>
        /// Sets how long the cache entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        /// <param name="options"></param>
        /// <param name="offset"></param>
        public static MemoryCacheEntryOptions SetSlidingExpiration(
            this MemoryCacheEntryOptions options,
            TimeSpan offset)
        {
            options.SlidingExpiration = offset;
            return options;
        }

        /// <summary>
        /// The given callback will be fired after the cache entry is evicted from the cache.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        public static MemoryCacheEntryOptions RegisterPostEvictionCallback(
            this MemoryCacheEntryOptions options,
            PostEvictionDelegate callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            return options.RegisterPostEvictionCallback(callback, state: null);
        }

        /// <summary>
        /// The given callback will be fired after the cache entry is evicted from the cache.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        public static MemoryCacheEntryOptions RegisterPostEvictionCallback(
            this MemoryCacheEntryOptions options,
            PostEvictionDelegate callback,
            object state)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            options.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
            {
                EvictionCallback = callback,
                State = state
            });
            return options;
        }

        /// <summary>
        /// Adds inherited trigger and absolute expiration information.
        /// </summary>
        /// <param name="link"></param>
        public static MemoryCacheEntryOptions AddEntryLink(this MemoryCacheEntryOptions options, IEntryLink link)
        {
            foreach (var trigger in link.Triggers)
            {
                options.AddExpirationTrigger(trigger);
            }

            if (link.AbsoluteExpiration.HasValue)
            {
                options.SetAbsoluteExpiration(link.AbsoluteExpiration.Value);
            }
            return options;
        }
    }
}