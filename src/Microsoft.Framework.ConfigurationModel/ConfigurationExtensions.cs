// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Framework.ConfigurationModel.Helper;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.ConfigurationModel
{
    public static class ConfigurationExtensions
    {
#if NET45 || DNX451 || DNXCORE50
        /// <summary>
        /// Adds the INI configuration source at <paramref name="path"/> to <paramref name="configuraton"/>.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfigurationSourceRoot"/> to add to.</param>
        /// <param name="path">Absolute path or path relative to <see cref="IConfigurationSourceRoot.BasePath"/> of
        /// <paramref name="configuration"/>.</param>
        /// <returns>The <see cref="IConfigurationSourceRoot"/>.</returns>
        public static IConfigurationSourceRoot AddIniFile([NotNull] this IConfigurationSourceRoot configuration, string path)
        {
            return AddIniFile(configuration, path, optional: false);
        }

        /// <summary>
        /// Adds the JSON configuration source at <paramref name="path"/> to <paramref name="configuraton"/>.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfigurationSourceRoot"/> to add to.</param>
        /// <param name="path">Absolute path or path relative to <see cref="IConfigurationSourceRoot.BasePath"/> of
        /// <paramref name="configuration"/>.</param>
        /// <param name="optional">Determines if loading the configuration source is optional.</param>
        /// <returns>The <see cref="IConfigurationSourceRoot"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="path"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">If <paramref name="optional"/> is <c>false</c> and the file cannot be resolved.</exception>
        public static IConfigurationSourceRoot AddIniFile([NotNull] this IConfigurationSourceRoot configuration, string path, bool optional)
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

            configuration.Add(new IniFileConfigurationSource(fullPath, optional: optional));
            return configuration;
        }
#endif

        public static IConfigurationSourceRoot AddCommandLine(this IConfigurationSourceRoot configuration, string[] args)
        {
            configuration.Add(new CommandLineConfigurationSource(args));
            return configuration;
        }

        public static IConfigurationSourceRoot AddCommandLine(this IConfigurationSourceRoot configuration, string[] args, IDictionary<string, string> switchMappings)
        {
            configuration.Add(new CommandLineConfigurationSource(args, switchMappings));
            return configuration;
        }

        public static IConfigurationSourceRoot AddEnvironmentVariables(this IConfigurationSourceRoot configuration)
        {
            configuration.Add(new EnvironmentVariablesConfigurationSource());
            return configuration;
        }

        public static IConfigurationSourceRoot AddEnvironmentVariables(this IConfigurationSourceRoot configuration, string prefix)
        {
            configuration.Add(new EnvironmentVariablesConfigurationSource(prefix));
            return configuration;
        }
    }
}
