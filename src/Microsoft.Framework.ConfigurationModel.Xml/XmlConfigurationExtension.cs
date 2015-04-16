// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.ConfigurationModel.Helper;
using Microsoft.Framework.ConfigurationModel.Xml;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.ConfigurationModel
{
    /// <summary>
    /// Extension methods for adding <see cref="XmlConfigurationSource"/>.
    /// </summary>
    public static class XmlConfigurationExtension
    {
        /// <summary>
        /// Adds the XML configuration source at <paramref name="path"/> to <paramref name="configuraton"/>.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfigurationSourceRoot"/> to add to.</param>
        /// <param name="path">Absolute path or path relative to <see cref="IConfigurationSourceRoot.BasePath"/> of
        /// <paramref name="configuration"/>.</param>
        /// <returns>The <see cref="IConfigurationSourceRoot"/>.</returns>
        public static IConfigurationSourceRoot AddXmlFile([NotNull] this IConfigurationSourceRoot configuration, string path)
        {
            return AddXmlFile(configuration, path, optional: false);
        }

        /// <summary>
        /// Adds the XML configuration source at <paramref name="path"/> to <paramref name="configuraton"/>.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfigurationSourceRoot"/> to add to.</param>
        /// <param name="path">Absolute path or path relative to <see cref="IConfigurationSourceRoot.BasePath"/> of
        /// <paramref name="configuration"/>.</param>
        /// <param name="optional">Determines if loading the configuration source is optional.</param>
        /// <returns>The <see cref="IConfigurationSourceRoot"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="path"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">If <paramref name="optional"/> is <c>false</c> and the file cannot be resolved.</exception>
        public static IConfigurationSourceRoot AddXmlFile([NotNull] this IConfigurationSourceRoot configuration, string path, bool optional)
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

            configuration.Add(new XmlConfigurationSource(fullPath, optional: optional));

            return configuration;
        }
    }
}
