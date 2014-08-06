// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.MemoryCache
{
    [AssemblyNeutral]
    public interface ICacheAddContext
    {
        /// <summary>
        /// The state passed into GetOrAdd.
        /// </summary>
        object State { get; }

        void SetPriority(CachePreservationPriority priority);

        void SetAbsoluteExpiration(DateTime absoulte);

        void SetAbsoluteExpiration(TimeSpan relative);

        void SetSlidingExpiraiton(TimeSpan offset);

        /// <summary>
        /// Expire this entry if the given event occures.
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