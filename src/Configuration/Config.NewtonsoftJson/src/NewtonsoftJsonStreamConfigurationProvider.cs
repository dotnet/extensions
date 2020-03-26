// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Extensions.Configuration.NewtonsoftJson
{
    /// <summary>
    /// Loads configuration key/values from a json stream into a provider.
    /// </summary>
    public class NewtonsoftJsonStreamConfigurationProvider : StreamConfigurationProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">The source of configuration.</param>
        public NewtonsoftJsonStreamConfigurationProvider(NewtonsoftJsonStreamConfigurationSource source) : base(source) { }

        /// <summary>
        /// Loads json configuration key/values from a stream into a provider.
        /// </summary>
        /// <param name="stream">The json <see cref="Stream"/> to load configuration data from.</param>
        public override void Load(Stream stream)
        {
            Data = NewtonsoftJsonConfigurationFileParser.Parse(stream);
        }
    }
}
