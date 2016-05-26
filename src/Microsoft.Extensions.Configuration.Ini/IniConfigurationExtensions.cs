// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.Configuration.Ini;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration
{
    public static class IniConfigurationExtensions
    {
        /// <summary>
        /// Adds the INI configuration provider at <paramref name="path"/> to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="path">Path relative to the base path stored in
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddIniFile(this IConfigurationBuilder builder, string path)
        {
            return AddIniFile(builder, provider: null, path: path, optional: false, reloadOnChange: false);
        }

        /// <summary>
        /// Adds the INI configuration provider at <paramref name="path"/> to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="path">Path relative to the base path stored in 
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
        /// <param name="optional">Whether the file is optional.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddIniFile(this IConfigurationBuilder builder, string path, bool optional)
        {
            return AddIniFile(builder, provider: null, path: path, optional: optional, reloadOnChange: false);
        }

        /// <summary>
        /// Adds the INI configuration provider at <paramref name="path"/> to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="path">Path relative to the base path stored in 
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
        /// <param name="optional">Whether the file is optional.</param>
        /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddIniFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange)
        {
            return AddIniFile(builder, provider: null, path: path, optional: optional, reloadOnChange: reloadOnChange);
        }

        /// <summary>
        /// Adds a INI configuration source to <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="provider">The <see cref="IFileProvider"/> to use to access the file.</param>
        /// <param name="path">Path relative to the base path stored in 
        /// <see cref="IConfigurationBuilder.Properties"/> of <paramref name="builder"/>.</param>
        /// <param name="optional">Whether the file is optional.</param>
        /// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddIniFile(this IConfigurationBuilder builder, IFileProvider provider, string path, bool optional, bool reloadOnChange)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(Resources.Error_InvalidFilePath, nameof(path));
            }

            if (provider == null && Path.IsPathRooted(path))
            {
                provider = new PhysicalFileProvider(Path.GetDirectoryName(path));
                path = Path.GetFileName(path);
            } 
            var source = new IniConfigurationSource
            {
                FileProvider = provider,
                Path = path,
                Optional = optional,
                ReloadOnChange = reloadOnChange
            };
            builder.Add(source);
            return builder;
        }
    }
}
