// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Framework.Configuration.Json
{
    public class FileConfigurationBuilderExtensionsTest
    {
        [Fact]
        public void SetBasePath_ThrowsIfBasePathIsNull()
        {
            // Arrange
            var builder = new ConfigurationBuilder();

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => builder.SetBasePath(null));
            Assert.Equal("basePath", ex.ParamName);
        }

        [Fact]
        public void SetBasePath_CheckPropertiesValueOnBuilder()
        {
            var expectedBasePath = @"C:\ExamplePath";
            var builder = new ConfigurationBuilder();

            builder.SetBasePath(expectedBasePath);
            Assert.Equal(expectedBasePath, builder.Properties["BasePath"]);
        }

        [Fact]
        public void GetBasePath_ReturnBaseBathIfSet()
        {
            // Arrange
            var testDir = Path.GetDirectoryName(Path.GetTempFileName());
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(testDir);

            // Act
            var actualPath = builder.GetBasePath();

            // Assert
            Assert.Equal(testDir, actualPath);
        }

        [Fact]
        public void GetBasePath_ReturnEmptyIfNotSet()
        {
            // Arrange
            var builder = new ConfigurationBuilder();

            // Act
            var actualPath = builder.GetBasePath();

            // Assert
            Assert.Equal(string.Empty, actualPath);
        }
    }
}
