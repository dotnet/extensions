// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Options.Infrastructure
{
    /// <summary>
    /// Uses ConfigureDefaultOptions to configure defaults for an options.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    public class ConfigureDefaults<TOptions> : IConfigureNamedOptions<TOptions> where TOptions : class
    {
        private readonly IEnumerable<ConfigureDefaultOptions<TOptions>> _defaults;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="defaults"></param>
        public ConfigureDefaults(IEnumerable<ConfigureDefaultOptions<TOptions>> defaults)
        {
            _defaults = defaults;
        }

        /// <summary>
        /// Invokes the registered configure Action if the name matches.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="options"></param>
        public virtual void Configure(string name, TOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            foreach (var configure in _defaults)
            {
                configure.Configure(name, options);
            }
        }

        /// <summary>
        /// Configures the default instance.
        /// </summary>
        /// <param name="options"></param>
        public void Configure(TOptions options) => Configure(Options.DefaultName, options);
    }
}