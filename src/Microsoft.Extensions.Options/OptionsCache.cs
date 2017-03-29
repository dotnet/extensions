// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Used to cache TOptions instances.
    /// </summary>
    /// <typeparam name="TOptions">The type of options being requested.</typeparam>
    public class OptionsCache<TOptions> : IOptionsCache<TOptions> where TOptions : class
    {
        private readonly ConcurrentDictionary<string, Lazy<TOptions>> _cache = new ConcurrentDictionary<string, Lazy<TOptions>>(StringComparer.Ordinal);

        public virtual TOptions GetOrAdd(string name, Func<TOptions> createOptions)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (createOptions == null)
            {
                throw new ArgumentNullException(nameof(createOptions));
            }
            return _cache.GetOrAdd(name, new Lazy<TOptions>(createOptions)).Value;
        }

        /// <summary>
        /// Tries to adds a new option to the cache, will return false if the name already exists.
        /// </summary>
        /// <param name="name">The name of the options instance.</param>
        /// <param name="options">The options instance.</param>
        /// <returns>Whether anything was added.</returns>
        public virtual bool TryAdd(string name, TOptions options)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            return _cache.TryAdd(name, new Lazy<TOptions>(() => options));
        }

        /// <summary>
        /// Try to remove an options instance.
        /// </summary>
        /// <param name="name">The name of the options instance.</param>
        /// <returns>Whether anything was removed.</returns>
        public virtual bool TryRemove(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            return _cache.TryRemove(name, out var ignored);
        }

        // Do we need a Clear all?
    }
}