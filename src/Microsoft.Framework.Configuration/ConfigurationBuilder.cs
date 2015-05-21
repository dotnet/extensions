// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Framework.Configuration
{
    public class ConfigurationBuilder : IConfigurationBuilder
    {
        private readonly IList<IConfigurationSource> _sources = new List<IConfigurationSource>();

        public ConfigurationBuilder(params IConfigurationSource[] sources)
            : this(null, sources)
        {
        }

        public ConfigurationBuilder(string basePath, params IConfigurationSource[] sources)
        {
            if (sources != null)
            {
                foreach (var singleSource in sources)
                {
                    Add(singleSource);
                }
            }

            BasePath = basePath;
        }

        public IEnumerable<IConfigurationSource> Sources
        {
            get
            {
                return _sources;
            }
        }

        public string BasePath
        {
            get;
        }

        /// <summary>
        /// Adds a new configuration source.
        /// </summary>
        /// <param name="configurationSource">The configuration source to add.</param>
        /// <returns>The same configuration source.</returns>
        public IConfigurationBuilder Add(IConfigurationSource configurationSource)
        {
            return Add(configurationSource, load: true);
        }

        /// <summary>
        /// Adds a new configuration source.
        /// </summary>
        /// <param name="configurationSource">The configuration source to add.</param>
        /// <param name="load">If true, the configuration source's <see cref="IConfigurationSource.Load"/> method will
        ///  be called.</param>
        /// <returns>The same configuration source.</returns>
        /// <remarks>This method is intended only for test scenarios.</remarks>
        public IConfigurationBuilder Add(IConfigurationSource configurationSource, bool load)
        {
            if (load)
            {
                configurationSource.Load();
            }
            _sources.Add(configurationSource);
            return this;
        }

        public IConfiguration Build()
        {
            return new ConfigurationSection(_sources);
        }
    }
}
