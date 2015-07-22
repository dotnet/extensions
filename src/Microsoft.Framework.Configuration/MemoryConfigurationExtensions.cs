// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Configuration.Memory;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.Configuration
{
    public static class MemoryConfigurationExtensions
    {
        /// <summary>
        /// Adds the memory configuration source to <paramref name="configuraton"/>.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddInMemoryCollection([NotNull] this IConfigurationBuilder configuration)
        {
            configuration.Add(new MemoryConfigurationSource());
            return configuration;
        }

        /// <summary>
        /// Adds the memory configuration source to <paramref name="configuraton"/>.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="initialData">The data to add to memory configuration source.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddInMemoryCollection(
            [NotNull] this IConfigurationBuilder configuration,
            IEnumerable<KeyValuePair<string, string>> initialData)
        {
            configuration.Add(new MemoryConfigurationSource(initialData));
            return configuration;
        }
    }
}
