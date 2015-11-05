// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Used to build key/value based configuration settings for use in an application.
    /// </summary>
    public class ConfigurationBuilder : IConfigurationBuilder
    {
        private readonly IList<IConfigurationProvider> _providers = new List<IConfigurationProvider>();

        /// <summary>
        /// Returns the providers used to obtain configuation values.
        /// </summary>
        public IEnumerable<IConfigurationProvider> Providers
        {
            get
            {
                return _providers;
            }
        }

        /// <summary>
        /// Gets a key/value collection that can be used to share data between the <see cref="IConfigurationBuilder"/>
        /// and the registered <see cref="IConfigurationProvider"/>s.
        /// </summary>
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Adds a new configuration provider.
        /// </summary>
        /// <param name="provider">The configuration provider to add.</param>
        /// <returns>The same <see cref="IConfigurationBuilder"/>.</returns>
        public IConfigurationBuilder Add(IConfigurationProvider provider)
        {
            return Add(provider, load: true);
        }

        /// <summary>
        /// Adds a new provider to obtain configuration values from.
        /// This method is intended only for test scenarios.
        /// </summary>
        /// <param name="provider">The configuration provider to add.</param>
        /// <param name="load">If true, the configuration provider's <see cref="IConfigurationProvider.Load"/> method will
        ///  be called.</param>
        /// <returns>The same <see cref="IConfigurationBuilder"/>.</returns>
        public IConfigurationBuilder Add(IConfigurationProvider provider, bool load)
        {
            if (load)
            {
                provider.Load();
            }
            _providers.Add(provider);
            return this;
        }

        /// <summary>
        /// Builds an <see cref="IConfiguration"/> with keys and values from the set of providers registered in
        /// <see cref="Providers"/>.
        /// </summary>
        /// <returns>An <see cref="IConfigurationRoot"/> with keys and values from the registered providers.</returns>
        public IConfigurationRoot Build()
        {
            return new ConfigurationRoot(_providers);
        }
    }
}
