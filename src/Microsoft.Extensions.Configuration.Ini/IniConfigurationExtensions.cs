// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.Configuration.Ini;

namespace Microsoft.Extensions.Configuration
{
    public static class IniConfigurationExtensions
    {
        /// <summary>
        /// Adds the INI configuration provider at <paramref name="path"/> to <paramref name="configurationBuilder"/>.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="path">Absolute path or path relative to the base path store in 
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="configurationBuilder"/>.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddIniFile(this IConfigurationBuilder configurationBuilder, string path)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            return AddIniFile(configurationBuilder, path, optional: false);
        }

        /// <summary>
        /// Adds the INI configuration provider at <paramref name="path"/> to <paramref name="configurationBuilder"/>.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="path">Absolute path or path relative to the base path store in 
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="configurationBuilder"/>.</param>
        /// <param name="optional">Determines if loading the configuration provider is optional.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="path"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">If <paramref name="optional"/> is <c>false</c> and the file cannot
        /// be resolved.</exception>
        public static IConfigurationBuilder AddIniFile(
            this IConfigurationBuilder configurationBuilder,
            string path,
            bool optional)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(Resources.Error_InvalidFilePath, nameof(path));
            }

            var fullPath = Path.Combine(configurationBuilder.GetBasePath(), path);

            if (!optional && !File.Exists(fullPath))
            {
                throw new FileNotFoundException(Resources.FormatError_FileNotFound(fullPath), fullPath);
            }

            configurationBuilder.Add(new IniConfigurationProvider(fullPath, optional: optional));
            return configurationBuilder;
        }
    }
}
