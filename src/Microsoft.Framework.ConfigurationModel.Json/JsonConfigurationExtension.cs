// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.ConfigurationModel.Helper;
using Microsoft.Framework.ConfigurationModel.Json;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.ConfigurationModel
{
    /// <summary>
    /// Extension methods for adding <see cref="JsonConfigurationSource"/>.
    /// </summary>
    public static class JsonConfigurationExtension
    {
        /// <summary>
        /// Adds the JSON configuration source at <paramref name="path"/> to <paramref name="configuraton"/>.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="path">Absolute path or path relative to <see cref="IConfigurationBuilder.BasePath"/> of
        /// <paramref name="configuration"/>.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddJsonFile(
            [NotNull] this IConfigurationBuilder configuration,
            string path)
        {
            return AddJsonFile(configuration, path, optional: false);
        }

        /// <summary>
        /// Adds the JSON configuration source at <paramref name="path"/> to <paramref name="configuraton"/>.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="path">Absolute path or path relative to <see cref="IConfigurationBuilder.BasePath"/> of
        /// <paramref name="configuration"/>.</param>
        /// <param name="optional">Determines if loading the configuration source is optional.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="path"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">If <paramref name="optional"/> is <c>false</c> and the file cannot
        /// be resolved.</exception>
        public static IConfigurationBuilder AddJsonFile(
            [NotNull] this IConfigurationBuilder configuration,
            string path,
            bool optional)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(Resources.Error_InvalidFilePath, nameof(path));
            }

            var fullPath = ConfigurationHelper.ResolveConfigurationFilePath(configuration, path);

            if (!optional && !File.Exists(fullPath))
            {
                throw new FileNotFoundException(Resources.FormatError_FileNotFound(fullPath), fullPath);
            }

            configuration.Add(new JsonConfigurationSource(fullPath, optional: optional));

            return configuration;
        }
    }
}
