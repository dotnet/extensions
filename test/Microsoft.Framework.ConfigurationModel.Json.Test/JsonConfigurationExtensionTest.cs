// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Framework.ConfigurationModel.Json
{
    public class JsonConfigurationExtensionTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AddJsonFile_ThrowsIfFilePathIsNullOrEmpty(string path)
        {
            // Arrange
            var configurationSource = new Configuration();

            // Act and Assert
            var ex = Assert.Throws<ArgumentException>(() => JsonConfigurationExtension.AddJsonFile(configurationSource, path));
            Assert.Equal("path", ex.ParamName);
            Assert.StartsWith("File path must be a non-empty string.", ex.Message);
        }

        [Fact]
        public void AddJsonFile_ThrowsIfFileDoesNotExistAtPath()
        {
            // Arrange
            var path = Path.Combine(Directory.GetCurrentDirectory(), "file-does-not-exist.json");
            var configurationSource = new Configuration();

            // Act and Assert
            var ex = Assert.Throws<FileNotFoundException>(() => JsonConfigurationExtension.AddJsonFile(configurationSource, path));
            Assert.Equal($"The configuration file '{path}' was not found and is not optional.", ex.Message);
        }
    }
}
