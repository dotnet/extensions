// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration
{
    public static class FileConfigurationExtensions
    {
        private static string FileProviderKey = "FileProvider";
        private static string BasePathKey = "BasePath";

        /// <summary>
        /// Sets the default <see cref="IFileProvider"/> to be used for file-based providers.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="fileProvider">The default file provider instance.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder SetFileProvider(this IConfigurationBuilder builder, IFileProvider fileProvider)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (fileProvider == null)
            {
                throw new ArgumentNullException(nameof(fileProvider));
            }

            builder.Properties[FileProviderKey] = fileProvider;
            return builder;
        }

        /// <summary>
        /// Gets the default <see cref="IFileProvider"/> to be used for file-based providers.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IFileProvider GetFileProvider(this IConfigurationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            object provider;
            if (builder.Properties.TryGetValue(FileProviderKey, out provider))
            {
                return builder.Properties[FileProviderKey] as IFileProvider;
            }

            return new PhysicalFileProvider(builder.GetBasePath());
        }

        /// <summary>
        /// Sets the FileProvider for file-based providers to a PhysicalFileProvider with the base path.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="basePath">The absolute path of file-based providers.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder SetBasePath(this IConfigurationBuilder builder, string basePath)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (basePath == null)
            {
                throw new ArgumentNullException(nameof(basePath));
            }

            builder.Properties[BasePathKey] = basePath;
            return builder.SetFileProvider(new PhysicalFileProvider(basePath));
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
            if (configurationBuilder.Properties.TryGetValue(BasePathKey, out basePath))
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
