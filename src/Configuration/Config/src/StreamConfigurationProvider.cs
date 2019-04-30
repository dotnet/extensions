// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Stream based configuration provider
    /// </summary>
    public class StreamConfigurationProvider : ConfigurationProvider
    {
        /// <summary>
        /// The source settings for this provider.
        /// </summary>
        public StreamConfigurationSource Source { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        public StreamConfigurationProvider(StreamConfigurationSource source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            if (Source.Loader == null)
            {
                throw new ArgumentNullException(nameof(Source.Loader));
            }
            // Review: should we allow null streams? Seems reasonable to defer default behavior to loader.
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Load()
            => Source.Loader.Load(this, Source.Stream);
    }
}
