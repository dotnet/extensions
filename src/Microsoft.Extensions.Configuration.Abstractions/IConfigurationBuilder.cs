// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Represents a type used to build application configuration.
    /// </summary>
    public interface IConfigurationBuilder
    {
        /// <summary>
        /// Gets a key/value collection that can be used to share data between the <see cref="IConfigurationBuilder"/>
        /// and the registered <see cref="IConfigurationProvider"/>s.
        /// </summary>
        Dictionary<string, object> Properties { get; }

        /// <summary>
        /// Gets the providers used to obtain configuation values
        /// </summary>
        IEnumerable<IConfigurationProvider> Providers { get; }

        /// <summary>
        /// Adds a new configuration provider.
        /// </summary>
        /// <param name="provider">The configuration provider to add.</param>
        /// <returns>The same <see cref="IConfigurationBuilder"/>.</returns>
        IConfigurationBuilder Add(IConfigurationProvider provider);

        /// <summary>
        /// Builds an <see cref="IConfiguration"/> with keys and values from the set of providers registered in
        /// <see cref="Providers"/>.
        /// </summary>
        /// <returns>An <see cref="IConfigurationRoot"/> with keys and values from the registered providers.</returns>
        IConfigurationRoot Build();
    }
}