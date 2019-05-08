// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Stream based configuration provider
    /// </summary>
    public abstract class StreamConfigurationProvider : ConfigurationProvider
    {
        /// <summary>
        /// The source settings for this provider.
        /// </summary>
        public StreamConfigurationSource Source { get; set; }

        private bool _loaded;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        public StreamConfigurationProvider(StreamConfigurationSource source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        public abstract void Load(Stream stream);

        /// <summary>
        /// 
        /// </summary>
        public override void Load()
        {
            if (_loaded)
            {
                throw new InvalidOperationException("StreamConfigurationProviders cannot be loaded more than once.");
            }
            Load(Source.Stream);
            _loaded = true;
        }
    }
}
