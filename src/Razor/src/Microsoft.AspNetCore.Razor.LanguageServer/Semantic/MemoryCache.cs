// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Services
{
    // We've created our own MemoryCache here, ideally we would use the one in Microsoft.Extensions.Caching.Memory,
    // but until we update O# that causes an Assembly load problem.
    internal class MemoryCache<TResult>
    {
        protected virtual int SizeLimit { get; } = 50;

        protected IDictionary<string, CacheEntry> _dict;

        public MemoryCache()
        {
            _dict = new ConcurrentDictionary<string, CacheEntry>(concurrencyLevel: 2, capacity: SizeLimit);
        }

        public TResult Get(string key)
        {
            _dict.TryGetValue(key, out var value);

            if (value != null)
            {
                value.LastAccess = DateTime.UtcNow;
            }

            return value.Result;
        }

        public void Set(string key, TResult value)
        {
            if (_dict.Count >= SizeLimit)
            {
                Compact();
            }

            _dict.Add(key, new CacheEntry
            {
                LastAccess = DateTime.UtcNow,
                Result = value,
            });
        }

        protected virtual void Compact()
        {
            var kvps = _dict.OrderBy(x => x.Value.LastAccess);

            for (var i = 0; i < SizeLimit / 2; i++)
            {
                _dict.Remove(kvps.ElementAt(i).Key);
            }
        }

        protected class CacheEntry
        {
            public TResult Result { get; set; }

            public DateTime LastAccess { get; set; }
        }
    }
}
