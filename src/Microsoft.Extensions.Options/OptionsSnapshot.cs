// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implementation of IOptionsSnapshot.
    /// </summary>
    /// <typeparam name="TOptions">The type of options being requested.</typeparam>
    public class OptionsSnapshot<TOptions> : IOptionsSnapshot<TOptions> where TOptions : class, new()
    {
        private readonly IOptionsFactory<TOptions> _factory;
        private readonly OptionsCache<TOptions> _cache = new OptionsCache<TOptions>(); // Note: this is a private cache

        /// <summary>
        /// Initializes a new instance with the specified options configurations.
        /// </summary>
        /// <param name="factory">The factory to use to create options.</param>
        public OptionsSnapshot(IOptionsFactory<TOptions> factory)
        {
            _factory = factory;
        }

        public TOptions Value
        {
            get
            {
                return Get(Options.DefaultName);
            }
        }

        public virtual TOptions Get(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            // Store the options in our instance cache
            return _cache.GetOrAdd(name, () => _factory.Create(name));
        }
    }
}
