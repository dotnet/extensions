// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implementation of IOptionsSnapshot.
    /// </summary>
    /// <typeparam name="TOptions">The type of options being requested.</typeparam>
    public class OptionsSnapshot<TOptions> : IOptionsSnapshot<TOptions> where TOptions : class, new()
    {
        private readonly IOptionsCache<TOptions> _cache;
        private readonly IOptionsFactory<TOptions> _factory;
        private readonly ConcurrentDictionary<string, TOptions> _snapshots = new ConcurrentDictionary<string, TOptions>();

        /// <summary>
        /// Initializes a new instance with the specified options configurations.
        /// </summary>
        /// <param name="cache">The cache to use.</param>
        /// <param name="factory">The factory to use to create options.</param>
        /// <param name="changeSources">The change token sources used to detect options changes.</param>
        public OptionsSnapshot(IOptionsCache<TOptions> cache, IOptionsFactory<TOptions> factory, IEnumerable<IOptionsChangeTokenSource<TOptions>> changeSources)
        {
            _cache = cache;
            _factory = factory;

            foreach (var source in changeSources)
            {
                ChangeToken.OnChange(
                    () => source.GetChangeToken(),
                    () => ClearCache(source.Name));
            }
        }

        public TOptions Value
        {
            get
            {
                return Get(Options.DefaultName);
            }
        }

        private void ClearCache(string name)
        {
            // Default to 
            if (string.IsNullOrEmpty(name))
            {
                name = Options.DefaultName;
            }
            _cache.TryRemove(name);
        }

        private TOptions GetOrAddCache(string name)
            => _cache.GetOrAdd(name, () => _factory.Create(name));

        public virtual TOptions Get(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            // Take a snapshot of the options value from the cache.
            return _snapshots.GetOrAdd(name, GetOrAddCache(name));
        }
    }
}
