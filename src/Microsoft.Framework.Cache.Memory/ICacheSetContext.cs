// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Expiration.Interfaces;

namespace Microsoft.Framework.Cache.Memory
{
    [AssemblyNeutral]
    public interface ICacheSetContext
    {
        /// <summary>
        /// The key identifying this entry.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// The state passed into Set. This can be used to avoid closures.
        /// </summary>
        object State { get; }

        /// <summary>
        /// Sets the priority for keeping this entry in the cache during a memory pressure triggered cleanup.
        /// </summary>
        /// <param name="priority"></param>
        void SetPriority(CachePreservationPriority priority);

        /// <summary>
        /// Sets an absolute expiration date for this entry.
        /// </summary>
        /// <param name="absolute"></param>
        void SetAbsoluteExpiration(DateTimeOffset absolute);

        /// <summary>
        /// Sets an absolute expiration time, relative to now.
        /// </summary>
        /// <param name="relative"></param>
        void SetAbsoluteExpiration(TimeSpan relative);

        /// <summary>
        /// Sets how long this entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        /// <param name="offset"></param>
        void SetSlidingExpiration(TimeSpan offset);

        /// <summary>
        /// Expire this entry if the given event occurs.
        /// </summary>
        void AddExpirationTrigger(IExpirationTrigger trigger);

        /// <summary>
        /// The given callback will be fired after this entry is evicted from the cache.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        void RegisterPostEvictionCallback(Action<string, object, EvictionReason, object> callback, object state);
    }
}