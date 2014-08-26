// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.MemoryCache
{
    internal class CacheAddContext : ICacheAddContext
    {
        internal CacheAddContext(string key)
        {
            Key = key;
        }

        public string Key { get; private set; }

        public object State { get; internal set; }

        internal DateTime CreationTime { get; set; }

        internal DateTime? AbsoluteExpiration { get; private set; }

        internal TimeSpan? SlidingExpiration { get; private set; }

        internal IList<IExpirationTrigger> Triggers { get; set; }

        public void SetPriority(CachePreservationPriority priority)
        {
            throw new NotImplementedException();
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

        public void RegisterPostEvictionCallback(Action<string, object, EvictionReason, object> callback, object state)
        {
            throw new NotImplementedException();
        }
    }
}