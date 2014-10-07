// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Cache.Distributed
{
    public static class CacheExtensions
    {
        public static byte[] Set(this IDistributedCache cache, string key, byte[] value)
        {
            return cache.Set(key, state: value, create: context => (byte[])context.State);
        }

        public static byte[] Set(this IDistributedCache cache, string key, Func<ICacheContext, byte[]> create)
        {
            return cache.Set(key, state: null, create: create);
        }

        public static byte[] Get(this IDistributedCache cache, string key)
        {
            byte[] value = null;
            cache.TryGetValue(key, out value);
            return value;
        }

        public static byte[] GetOrSet(this IDistributedCache cache, string key, byte[] value)
        {
            byte[] value1;
            if (cache.TryGetValue(key, out value1))
            {
                return value1;
            }
            return cache.Set(key, value);
        }

        public static byte[] GetOrSet(this IDistributedCache cache, string key, Func<ICacheContext, byte[]> create)
        {
            byte[] value;
            if (cache.TryGetValue(key, out value))
            {
                return value;
            }
            return cache.Set(key, create);
        }

        public static byte[] GetOrSet(this IDistributedCache cache, string key, object state, Func<ICacheContext, byte[]> create)
        {
            byte[] value;
            if (cache.TryGetValue(key, out value))
            {
                return value;
            }
            return cache.Set(key, state, create);
        }
    }
}