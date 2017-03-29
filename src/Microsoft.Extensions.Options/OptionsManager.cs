// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implementation of IOptions.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    public class OptionsManager<TOptions> : IOptions<TOptions> where TOptions : class, new()
    {
        private LegacyOptionsCache<TOptions> _optionsCache;

        /// <summary>
        /// Initializes a new instance with the specified options configurations.
        /// </summary>
        /// <param name="setups">The configuration actions to run.</param>
        public OptionsManager(IEnumerable<IConfigureOptions<TOptions>> setups)
        {
            _optionsCache = new LegacyOptionsCache<TOptions>(setups);
        }

        /// <summary>
        /// The configured options instance.
        /// </summary>
        public virtual TOptions Value
        {
            get
            {
                return _optionsCache.Value;
            }
        }
    }
}