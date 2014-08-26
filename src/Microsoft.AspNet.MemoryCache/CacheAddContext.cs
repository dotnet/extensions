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
            throw new NotImplementedException();
        }

        public void SetAbsoluteExpiration(DateTime absoulte)
        {
            throw new NotImplementedException();
        }

        public void SetSlidingExpiraiton(TimeSpan offset)
        {
            throw new NotImplementedException();
        }

        public void RegisterPostEvictionCallback(Action<string, object, EvictionReason, object> callback, object state)
        {
            throw new NotImplementedException();
        }
    }
}