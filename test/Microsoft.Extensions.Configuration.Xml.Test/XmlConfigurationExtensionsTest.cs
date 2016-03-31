// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Extensions.Configuration.Xml.Test
{
    public class XmlConfigurationExtensionsTest
    {
        [Fact]
        public void AddXmlFile_ThrowsIfConfigureSourceIsNull()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => configurationBuilder.AddXmlFile(configureSource: null));
            Assert.Equal("configureSource", ex.ParamName);
        }

        [Fact]
        public void AddXmlFile_ThrowsIfFileDoesNotExistAtPath()
        {
            var config = new ConfigurationBuilder().AddXmlFile(source =>
            {
                source.Path = "NotExistingConfig.xml";
                source.Optional = false;
            });
            // Arrange

            // Act and Assert
            var ex = Assert.Throws<FileNotFoundException>(() => config.Build());
            Assert.Equal($"The configuration file 'NotExistingConfig.xml' was not found and is not optional.", ex.Message);
        }
    }
}
