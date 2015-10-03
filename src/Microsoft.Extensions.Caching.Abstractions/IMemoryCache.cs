// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Caching.Memory
{
    public interface IMemoryCache : IDisposable
    {
        /// <summary>
        /// Creates an entry link scope.
        /// </summary>
        /// <returns>The <see cref="IEntryLink"/>.</returns>
        IEntryLink CreateLinkingScope();

        /// <summary>
        /// Create or overwrite an entry in the cache.
        /// </summary>
        /// <param name="key">An object identifying the entry.</param>
        /// <param name="value">The value to be cached.</param>
        /// <param name="options">The <see cref="MemoryCacheEntryOptions"/>.</param>
        /// <returns>The object that was cached.</returns>
        object Set(object key, object value, MemoryCacheEntryOptions options);

        /// <summary>
        /// Gets the item associated with this key if present.
        /// </summary>
        /// <param name="key">An object identifying the requested entry.</param>
        /// <param name="value">The located value or null.</param>
        /// <returns>True if the key was found.</returns>
        bool TryGetValue(object key, out object value);

        /// <summary>
        /// Removes the object associated with the given key.
        /// </summary>
        /// <param name="key">An object identifying the entry.</param>
        void Remove(object key);
    }
}