// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.Caching.Distributed
{
    public static class CacheExtensions
    {
        public static void Set(this IDistributedCache cache, [NotNull] string key, byte[] value)
        {
            cache.Set(key, value, new DistributedCacheEntryOptions());
        }

        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, byte[] value)
        {
            return cache.SetAsync(key, value, new DistributedCacheEntryOptions());
        }
    }
}