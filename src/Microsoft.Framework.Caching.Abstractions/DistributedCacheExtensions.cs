// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.Caching.Distributed
{
    /// <summary>
    /// Extension methods for setting data in an <see cref="IDistributedCache" />.
    /// </summary>
    public static class DistributedCacheExtensions
    {
        /// <summary>
        /// Sets a sequence of bytes in the specified cache with the specified key.
        /// </summary>
        /// <param name="cache">The cache in which to store the data.</param>
        /// <param name="key">The key to store the data in.</param>
        /// <param name="value">The data to store in the cache.</param>
        public static void Set(this IDistributedCache cache, [NotNull] string key, byte[] value)
        {
            cache.Set(key, value, new DistributedCacheEntryOptions());
        }

        /// <summary>
        /// Asynchronously sets a sequence of bytes in the specified cache with the specified key.
        /// </summary>
        /// <param name="cache">The cache in which to store the data.</param>
        /// <param name="key">The key to store the data in.</param>
        /// <param name="value">The data to store in the cache.</param>
        /// <returns>A task that represents the asynchronous set operation.</returns>
        public static Task SetAsync(this IDistributedCache cache, [NotNull] string key, byte[] value)
        {
            return cache.SetAsync(key, value, new DistributedCacheEntryOptions());
        }
    }
}