// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Extensions.Configuration.Json
{
    /// <summary>
    /// Loads configuration key/values from a json stream into a provider.
    /// </summary>
    public class JsonConfigurationStreamLoader : IConfigurationStreamLoader
    {
        /// <summary>
        /// Loads json configuration key/values from a stream into a provider.
        /// </summary>
        /// <param name="provider">The <see cref="IConfigurationProvider"/> to store the data.</param>
        /// <param name="stream">The json <see cref="Stream"/> to load configuration data from.</param>
        public void Load(IConfigurationProvider provider, Stream stream)
            => provider.Use(JsonConfigurationFileParser.Parse(stream));
    }
}
