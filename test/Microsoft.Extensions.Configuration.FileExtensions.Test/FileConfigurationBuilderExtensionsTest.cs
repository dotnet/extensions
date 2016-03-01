// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Extensions.Configuration.Json
{
    public class FileConfigurationBuilderExtensionsTest
    {
        [Fact]
        public void SetBasePath_ThrowsIfBasePathIsNull()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => configurationBuilder.SetBasePath(null));
            Assert.Equal("basePath", ex.ParamName);
        }

        [Fact]
        public void SetBasePath_CheckPropertiesValueOnBuilder()
        {
            var expectedBasePath = @"C:\ExamplePath";
            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.SetBasePath(expectedBasePath);
            Assert.Equal(expectedBasePath, configurationBuilder.Properties["BasePath"]);
        }

        [Fact]
        public void GetBasePath_ReturnBaseBathIfSet()
        {
            // Arrange
            var testDir = Path.GetDirectoryName(Path.GetTempFileName());
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(testDir);

            // Act
            var actualPath = configurationBuilder.GetBasePath();

            // Assert
            Assert.Equal(testDir, actualPath);
        }

        [Fact]
        public void GetBasePath_ReturnBaseDirectoryIfNotSet()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            // Act
            var actualPath = configurationBuilder.GetBasePath();

            string expectedPath;

#if NETSTANDARDAPP1_5
            expectedPath = AppContext.BaseDirectory;
#else
            expectedPath = Path.GetFullPath(AppDomain.CurrentDomain.GetData("APP_CONTEXT_BASE_DIRECTORY") as string ?? 
                AppDomain.CurrentDomain.BaseDirectory);
#endif

            // Assert
            Assert.Equal(expectedPath, actualPath);
        }
    }
}
