// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.IO;
using Microsoft.Framework.ConfigurationModel.Xml;

namespace Microsoft.Framework.ConfigurationModel
{
    public static class XmlConfigurationExtension
    {
        public static IConfigurationSourceRoot AddXmlFile(this IConfigurationSourceRoot configuration, string path)
        {
            return AddXmlFile(configuration, path, optional: false);
        }

        public static IConfigurationSourceRoot AddXmlFile(this IConfigurationSourceRoot configuration, string path, bool optional)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(Resources.Error_InvalidFilePath, "path");
            }

            var fullPath = PathResolver.ResolveAppRelativePath(path);

            if (!optional && !File.Exists(fullPath))
            {
                throw new FileNotFoundException(Resources.Error_FileNotFound, fullPath);
            }

            configuration.Add(new XmlConfigurationSource(path, optional: optional));

            return configuration;
        }
    }
}
