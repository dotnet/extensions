// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Loads configuration key/values from a stream into a provider.
    /// </summary>
    public interface IConfigurationStreamLoader
    {
        /// <summary>
        /// Loads configuration key/values from a stream into a provider.
        /// </summary>
        /// <param name="provider">The <see cref="IConfigurationProvider"/> to store the data.</param>
        /// <param name="stream">The <see cref="Stream"/> to load configuration data from.</param>
        void Load(IConfigurationProvider provider, Stream stream);
    }
}