// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Extensions.Configuration
{
    public static class FileConfigurationExtensions
    {
        /// <summary>
        /// Sets the base path to discover files in for file-based providers.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="basePath">The absolute path of file-based providers.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder SetBasePath(this IConfigurationBuilder configurationBuilder, string basePath)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            if (basePath == null)
            {
                throw new ArgumentNullException(nameof(basePath));
            }

            configurationBuilder.Properties["BasePath"] = basePath;

            return configurationBuilder;
        }

        /// <summary>
        /// Gets the base path to discover files in for file-based providers.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <returns>The base path.</returns>
        public static string GetBasePath(this IConfigurationBuilder configurationBuilder)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            object basePath;
            if (configurationBuilder.Properties.TryGetValue("BasePath", out basePath))
            {
                return (string)basePath;
            }

#if NET451
            var stringBasePath = AppDomain.CurrentDomain.GetData("APP_CONTEXT_BASE_DIRECTORY") as string ??
                AppDomain.CurrentDomain.BaseDirectory ?? 
                string.Empty;

            return Path.GetFullPath(stringBasePath);
#else
            return AppContext.BaseDirectory ?? string.Empty;
#endif
        }
    }
}
