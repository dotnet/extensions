// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.MemoryCache
{
    using EvictionCallback = Action<string, object, EvictionReason, object>;

    internal class CacheAddContext : ICacheAddContext
    {
        internal CacheAddContext(string key)
        {
            Key = key;
            Priority = CachePreservationPriority.Normal;
        }

        public string Key { get; private set; }

        public object State { get; internal set; }

        internal DateTime CreationTime { get; set; }

        internal DateTime? AbsoluteExpiration { get; private set; }

        internal TimeSpan? SlidingExpiration { get; private set; }

        internal IList<IExpirationTrigger> Triggers { get; set; }

        internal IList<Tuple<EvictionCallback, object>> PostEvictionCallbacks { get; set; }

        internal CachePreservationPriority Priority { get; private set; }

        public void SetPriority(CachePreservationPriority priority)
        {
            Priority = priority;
        }

        public void AddExpirationTrigger(IExpirationTrigger trigger)
        {
            if (Triggers == null)
            {
                Triggers = new List<IExpirationTrigger>(1);
            }
            Triggers.Add(trigger);
        }

        public void SetAbsoluteExpiration(TimeSpan relative)
        {
            if (relative <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("relative", relative, "The relative expriation value must be positive.");
            }
            AbsoluteExpiration = CreationTime + relative;
        }

        public void SetAbsoluteExpiration(DateTime absoulte)
        {
            AbsoluteExpiration = absoulte;
        }

        public void SetSlidingExpiraiton(TimeSpan offset)
        {
            if (offset <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("offset", offset, "The sliding expriation value must be positive.");
            }
            SlidingExpiration = offset;
        }

        public void RegisterPostEvictionCallback(EvictionCallback callback, object state)
        {
            if (PostEvictionCallbacks == null)
            {
                PostEvictionCallbacks = new List<Tuple<EvictionCallback, object>>(1);
            }
            PostEvictionCallbacks.Add(new Tuple<EvictionCallback, object>(callback, state));
        }
    }
}