// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Framework.ConfigurationModel.Xml.Test
{
    public class XmlConfigurationExtensionTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AddXmlFile_ThrowsIfFilePathIsNullOrEmpty(string path)
        {
            // Arrange
            var configurationSource = new Configuration();

            // Act and Assert
            var ex = Assert.Throws<ArgumentException>(() => XmlConfigurationExtension.AddXmlFile(configurationSource, path));
            Assert.Equal("path", ex.ParamName);
            Assert.StartsWith("File path must be a non-empty string.", ex.Message);
        }

        [Fact]
        public void AddXmlFile_ThrowsIfFileDoesNotExistAtPath()
        {
            // Arrange
            var path = Path.Combine(Directory.GetCurrentDirectory(), "file-does-not-exist.xml");
            var configurationSource = new Configuration();

            // Act and Assert
            var ex = Assert.Throws<FileNotFoundException>(() => XmlConfigurationExtension.AddXmlFile(configurationSource, path));
            Assert.Equal($"The configuration file '{path}' was not found and is not optional.", ex.Message);
        }
    }
}
