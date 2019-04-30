// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// IConfigurationBuilder extension methods for stream based configuration provider.
    /// </summary>
    public static class StreamBuilderExtensions
    {
        /// <summary>
        /// Adds a JSON configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="loader">The <see cref="IConfigurationStreamLoader"/> that will load the configuration data from the stream.</param>
        /// <param name="stream">The <see cref="Stream"/> to read the configuration data from.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddStream(this IConfigurationBuilder builder, IConfigurationStreamLoader loader, Stream stream)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.Add<StreamConfigurationSource>(s =>
            {
                s.Loader = loader;
                s.Stream = stream;
            });
        }
    }
}
