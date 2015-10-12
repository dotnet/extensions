// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    public class ConfigurationBuilder : IConfigurationBuilder
    {
        private readonly IList<IConfigurationProvider> _providers = new List<IConfigurationProvider>();

        public IEnumerable<IConfigurationProvider> Providers
        {
            get
            {
                return _providers;
            }
        }

        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Adds a new configuration provider.
        /// </summary>
        /// <param name="configurationProvider">The configuration provider to add.</param>
        /// <returns>The same configuration provider.</returns>
        public IConfigurationBuilder Add(IConfigurationProvider configurationProvider)
        {
            return Add(configurationProvider, load: true);
        }

        /// <summary>
        /// Adds a new configuration provider.
        /// </summary>
        /// <param name="configurationProvider">The configuration provider to add.</param>
        /// <param name="load">If true, the configuration provider's <see cref="IConfigurationProvider.Load"/> method will
        ///  be called.</param>
        /// <returns>The same configuration provider.</returns>
        /// <remarks>This method is intended only for test scenarios.</remarks>
        public IConfigurationBuilder Add(IConfigurationProvider configurationProvider, bool load)
        {
            if (load)
            {
                configurationProvider.Load();
            }
            _providers.Add(configurationProvider);
            return this;
        }

        public IConfigurationRoot Build()
        {
            return new ConfigurationRoot(_providers);
        }
    }
}
