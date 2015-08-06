// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Framework.Configuration.Helper;
using Xunit;

namespace Microsoft.Framework.Configuration.Test
{
    public class ConfigurationHelperTest
    {
        [Fact]
        public void ResolveFilePath()
        {
            var testFile = Path.GetTempFileName();
            var testDir = Path.GetDirectoryName(testFile);
            var testFileName = Path.GetFileName(testFile);
            var mockSourceRoot = new MockConfigurationBuilder { BasePath = testDir };

            var actualPath = ConfigurationHelper.ResolveConfigurationFilePath(mockSourceRoot, testFileName);

            Assert.Equal(testFile, actualPath);
        }

        [Fact]
        public void ThrowWhenBasePathIsNull()
        {
            var testFile = "config.j";
            var mockSourceRoot = new MockConfigurationBuilder();
            var expectErrorMessage = Resources.FormatError_MissingBasePath(
                testFile,
                typeof(IConfigurationBuilder).Name,
                nameof(mockSourceRoot.BasePath));

            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                ConfigurationHelper.ResolveConfigurationFilePath(mockSourceRoot, testFile);
            });

            Assert.Equal(expectErrorMessage, exception.Message);
        }

        [Fact]
        public void NotThrowWhenFileDoesNotExists()
        {
            var testFile = Path.GetTempFileName();
            var testDir = Path.GetDirectoryName(testFile);
            var testFileName = Path.GetFileName(testFile);
            var mockBuilder = new MockConfigurationBuilder { BasePath = testDir };

            File.Delete(testFile);

            var path = ConfigurationHelper.ResolveConfigurationFilePath(mockBuilder, testFileName);

            Assert.Equal(testFile, path);
        }

        private class MockConfigurationBuilder : IConfigurationBuilder
        {
            public string this[string key]
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public string BasePath
            {
                get; set;
            }

            public IEnumerable<IConfigurationSource> Sources
            {
                get { throw new NotImplementedException(); }
            }

            public IConfigurationBuilder Add(IConfigurationSource configurationSource)
            {
                throw new NotImplementedException();
            }

            public IConfiguration GetSection(string key)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<KeyValuePair<string, IConfiguration>> GetChildren()
            {
                throw new NotImplementedException();
            }

            public void Reload()
            {
                throw new NotImplementedException();
            }

            public IConfigurationRoot Build()
            {
                throw new NotImplementedException();
            }
        }
    }
}